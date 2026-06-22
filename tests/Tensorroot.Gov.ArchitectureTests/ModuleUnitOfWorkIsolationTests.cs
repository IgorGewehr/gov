using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;
using Xunit;

namespace Tensorroot.Gov.ArchitectureTests;

/// <summary>
/// Fitness Function de robustez (achado H5 / ADR-0010): a unidade de trabalho compartilhada confirma
/// UM único <see cref="ModuleDbContext"/> por escopo. Forçar dois contextos de MÓDULOS DISTINTOS no
/// mesmo escopo (ex.: Financas + Tributos) deve LANÇAR no <see cref="ScopeDbContextHolder"/> — em vez
/// de last-writer-wins, que descartaria silenciosamente as mutações do primeiro módulo. O fluxo
/// legítimo (OutboxBackgroundService) usa escopo por módulo, então nunca viola a guarda.
/// </summary>
public sealed class ModuleUnitOfWorkIsolationTests
{
    [Fact]
    public void Dois_modulos_no_mesmo_escopo_violam_a_unidade_de_trabalho_e_lancam()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        var holder = new ScopeDbContextHolder();
        var tenant = new TenantFake();

        var financasOptions = new DbContextOptionsBuilder<FinancasDbContext>().UseSqlite(conexao).Options;
        var tributosOptions = new DbContextOptionsBuilder<TributosDbContext>().UseSqlite(conexao).Options;

        // O contexto se registra no holder ao construir (como em runtime). O primeiro módulo entra OK.
        using var financas = new FinancasDbContext(financasOptions, tenant, holder);

        // O segundo módulo (Tributos) no MESMO escopo deve falhar-alto.
        var acao = () => new TributosDbContext(tributosOptions, tenant, holder);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*mesmo escopo*");
        holder.Atual.Should().BeSameAs(financas);
    }

    private sealed class TenantFake : ITenantContext
    {
        public Guid TenantId => Guid.Parse("99999999-9999-9999-9999-999999999999");

        public bool HasTenant => true;
    }
}
