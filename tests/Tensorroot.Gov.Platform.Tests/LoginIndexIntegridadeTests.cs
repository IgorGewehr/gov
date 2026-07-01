using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Platform.Persistence;
using Tensorroot.Gov.Platform.Tenancy;
using Xunit;

namespace Tensorroot.Gov.Platform.Tests;

/// <summary>
/// Bug hunt: integridade referencial do índice central de login (email → tenant). Registrar um índice
/// para um tenant inexistente criaria um órfão que faria o login falhar de forma indistinguível de
/// e-mail não cadastrado (o JOIN com Tenants retornaria null silenciosamente).
/// </summary>
public sealed class LoginIndexIntegridadeTests : IDisposable
{
    private readonly SqliteConnection _conexao;

    public LoginIndexIntegridadeTests()
    {
        _conexao = new SqliteConnection("DataSource=:memory:");
        _conexao.Open();
    }

    private PlatformDbContext Contexto()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseSqlite(_conexao).Options;
        var contexto = new PlatformDbContext(options);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    [Fact]
    public async Task Registrar_indice_para_tenant_inexistente_e_rejeitado()
    {
        await using var contexto = Contexto();
        var servico = new UsuarioTenantIndexService(contexto);

        var acao = () => servico.RegistrarAsync("fulano@x.gov.br", Guid.NewGuid(), CancellationToken.None);

        await acao.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Registrar_indice_para_tenant_existente_funciona()
    {
        await using var contexto = Contexto();
        var tenant = Tenant.Criar("11.222.333/0001-81", "Prefeitura", PoderTenant.Executivo);
        contexto.Tenants.Add(tenant);
        await contexto.SaveChangesAsync();

        var servico = new UsuarioTenantIndexService(contexto);
        await servico.RegistrarAsync("fulano@x.gov.br", tenant.Id, CancellationToken.None);

        (await contexto.UsuariosTenantIndex.CountAsync()).Should().Be(1);
    }

    public void Dispose() => _conexao.Dispose();
}
