using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Tributos.Application.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Picker do balcão (P1): busca de contribuintes por nome (LIKE) ou CPF/CNPJ (dígitos), com ISOLAMENTO
/// por tenant (Global Query Filter) e MÁSCARA LGPD do documento no resumo.
/// </summary>
public sealed class BuscarContribuintesTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("aaaa1111-1111-1111-1111-111111111111");
    private static readonly Guid TenantB = Guid.Parse("bbbb2222-2222-2222-2222-222222222222");

    private readonly SqliteConnection _connection;

    public BuscarContribuintesTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task Busca_por_nome_retorna_correspondentes_e_nao_vaza_outro_tenant()
    {
        await using (var ctx = CriarContexto(TenantA))
        {
            ctx.Contribuintes.Add(Contribuinte.PessoaFisica(TenantA, Cpf.Create("529.982.247-25"), "João da Silva"));
            ctx.Contribuintes.Add(Contribuinte.PessoaFisica(TenantA, Cpf.Create("111.444.777-35"), "Maria Souza"));
            ctx.Contribuintes.Add(Contribuinte.PessoaJuridica(TenantA, Cnpj.Create("11.222.333/0001-81"), "Padaria Souza LTDA"));
            await ctx.SaveChangesAsync();
        }

        await using (var ctxB = CriarContexto(TenantB))
        {
            ctxB.Contribuintes.Add(Contribuinte.PessoaFisica(TenantB, Cpf.Create("168.995.350-09"), "Souza de Outro Tenant"));
            await ctxB.SaveChangesAsync();
        }

        await using var contexto = CriarContexto(TenantA);
        var handler = new BuscarContribuintesHandler(new ContribuinteRepository(contexto));

        var resultado = await handler.Handle(new BuscarContribuintesQuery("Souza"), CancellationToken.None);

        resultado.Select(c => c.Nome).Should().BeEquivalentTo("Maria Souza", "Padaria Souza LTDA");
    }

    [Fact]
    public async Task Busca_por_documento_usa_os_digitos_e_mascara_no_resumo()
    {
        await using (var ctx = CriarContexto(TenantA))
        {
            ctx.Contribuintes.Add(Contribuinte.PessoaFisica(TenantA, Cpf.Create("529.982.247-25"), "João da Silva"));
            await ctx.SaveChangesAsync();
        }

        await using var contexto = CriarContexto(TenantA);
        var handler = new BuscarContribuintesHandler(new ContribuinteRepository(contexto));

        var resultado = await handler.Handle(new BuscarContribuintesQuery("529.982"), CancellationToken.None);

        var unico = resultado.Should().ContainSingle().Subject;
        unico.Nome.Should().Be("João da Silva");
        unico.DocumentoMascarado.Should().StartWith("529").And.EndWith("25");
        unico.DocumentoMascarado.Should().NotContain("982247");
    }

    private TributosDbContext CriarContexto(Guid tenantId)
    {
        var tenantContext = new TenantContextFakeBusca(tenantId);
        var options = new DbContextOptionsBuilder<TributosDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFakeBusca(), TimeProvider.System))
            .Options;

        var contexto = new TributosDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    public void Dispose() => _connection.Dispose();
}

file sealed class TenantContextFakeBusca(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

file sealed class CurrentUserFakeBusca : ICurrentUser
{
    public string? UserId => "teste-busca";

    public string? UserName => "Usuário Busca";

    public string? IpAddress => "127.0.0.1";
}
