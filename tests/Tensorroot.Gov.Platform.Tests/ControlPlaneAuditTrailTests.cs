using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.Platform.Persistence;
using Tensorroot.Gov.Platform.Tenancy;
using Xunit;

namespace Tensorroot.Gov.Platform.Tests;

/// <summary>
/// R3 — auditoria do CONTROL-PLANE: mutações do registro central (tenants, licenças, índice de login)
/// passam a ser SELADAS na trilha hash-chain (cadeia única sob Guid.Empty, schema "plataforma"),
/// quando antes ficavam fora de qualquer trilha.
/// </summary>
public sealed class ControlPlaneAuditTrailTests
{
    private const string CnpjValido = "11.222.333/0001-81";

    private static PlatformDbContext CriarContexto(SqliteConnection conexao)
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlite(conexao)
            .AddInterceptors(new AuditSaveChangesInterceptor(new CurrentUserFakePlatform(), TimeProvider.System))
            .Options;
        return new PlatformDbContext(options);
    }

    [Fact]
    public async Task Provisionar_tenant_gera_trilha_de_auditoria_selada()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        await using var contexto = CriarContexto(conexao);
        await contexto.Database.EnsureCreatedAsync();

        contexto.Tenants.Add(Tenant.Criar(CnpjValido, "Prefeitura de Exemplo", PoderTenant.Executivo));
        await contexto.SaveChangesAsync();

        var trilha = await contexto.AuditTrail.ToListAsync();

        var entrada = trilha.Should().ContainSingle().Subject;
        entrada.EntityName.Should().Contain("Tenant");
        entrada.Action.Should().Be("Added");
        entrada.Sequencia.Should().Be(1);
        entrada.HashAnterior.Should().Be(AuditHashChain.HashGenesis);
        entrada.HashAtual.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Mutacoes_sucessivas_encadeiam_a_trilha()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        await using var contexto = CriarContexto(conexao);
        await contexto.Database.EnsureCreatedAsync();

        var tenant = Tenant.Criar(CnpjValido, "Prefeitura de Exemplo", PoderTenant.Executivo);
        contexto.Tenants.Add(tenant);
        await contexto.SaveChangesAsync();

        tenant.Desativar();
        await contexto.SaveChangesAsync();

        var trilha = await contexto.AuditTrail.OrderBy(t => t.Sequencia).ToListAsync();

        trilha.Should().HaveCount(2);
        trilha[0].Sequencia.Should().Be(1);
        trilha[1].Sequencia.Should().Be(2);
        trilha[1].HashAnterior.Should().Be(trilha[0].HashAtual, "a 2ª linha encadeia no selo da 1ª");
    }
}

file sealed class CurrentUserFakePlatform : ICurrentUser
{
    public string? UserId => "operador-plataforma";

    public string? UserName => "Operador da Plataforma";

    public string? IpAddress => "127.0.0.1";
}
