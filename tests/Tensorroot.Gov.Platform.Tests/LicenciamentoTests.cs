using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Platform.Persistence;
using Tensorroot.Gov.Platform.Tenancy;
using Xunit;

namespace Tensorroot.Gov.Platform.Tests;

/// <summary>
/// Prova do núcleo comercial: um tenant só acessa os módulos que licenciou
/// (qualquer combinação é vendável), e licenças podem ser ativadas/desativadas.
/// </summary>
public sealed class LicenciamentoTests
{
    [Fact]
    public async Task Tenant_acessa_somente_modulos_licenciados_e_licenca_pode_ser_alternada()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseSqlite(conexao).Options;
        await using var contexto = new PlatformDbContext(options);
        await contexto.Database.EnsureCreatedAsync();

        var provisionamento = new TenantProvisioningService(
            contexto, new TenantConnectionCache(TimeProvider.System), ProvedorKekFake.Protetor());
        var provider = new TenantModuleProvider(contexto);

        // Prefeitura licencia SÓ Tributos + Saúde.
        var tenantId = await provisionamento.ProvisionarAsync(
            "11.222.333/0001-81", "Prefeitura de Exemplo", PoderTenant.Executivo,
            connectionString: null,
            new[] { "Tributos", "Saude" }, CancellationToken.None);

        (await provider.IsModuleEnabledAsync(tenantId, "Tributos", CancellationToken.None)).Should().BeTrue();
        (await provider.IsModuleEnabledAsync(tenantId, "Saude", CancellationToken.None)).Should().BeTrue();
        (await provider.IsModuleEnabledAsync(tenantId, "Financas", CancellationToken.None)).Should().BeFalse();
        (await provider.EnabledModulesAsync(tenantId, CancellationToken.None))
            .Should().BeEquivalentTo(new[] { "Tributos", "Saude" });

        // Desativa a licença de Saúde.
        await provisionamento.DefinirModuloAsync(tenantId, "Saude", ativo: false, CancellationToken.None);
        (await provider.IsModuleEnabledAsync(tenantId, "Saude", CancellationToken.None)).Should().BeFalse();

        // Ativa Finanças posteriormente.
        await provisionamento.DefinirModuloAsync(tenantId, "Financas", ativo: true, CancellationToken.None);
        (await provider.IsModuleEnabledAsync(tenantId, "Financas", CancellationToken.None)).Should().BeTrue();
    }
}
