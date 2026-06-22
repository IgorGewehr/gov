using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence;

/// <summary>
/// DbContext do modulo RecursosHumanos (schema isolado "recursoshumanos"), herdando Outbox,
/// Audit Trail e o Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Implementa
/// <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class RecursosHumanosDbContext(DbContextOptions<RecursosHumanosDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "recursoshumanos";

    /// <summary>Servidores (vinculos de pessoal: estatutarios RPPS e celetistas RGPS).</summary>
    public DbSet<Servidor> Servidores => Set<Servidor>();

    /// <summary>Cargos publicos da estrutura de pessoal.</summary>
    public DbSet<Cargo> Cargos => Set<Cargo>();

    /// <summary>Folhas de pagamento por competencia.</summary>
    public DbSet<FolhaDePagamento> FolhasDePagamento => Set<FolhaDePagamento>();

    /// <summary>Rubricas (verbas) parametrizaveis do catalogo do tenant (S-1010).</summary>
    public DbSet<RubricaFolha> Rubricas => Set<RubricaFolha>();

    /// <summary>Tabelas INSS (RGPS) parametrizadas por competencia.</summary>
    public DbSet<TabelaInss> TabelasInss => Set<TabelaInss>();

    /// <summary>Tabelas IRRF parametrizadas por competencia.</summary>
    public DbSet<TabelaIrrf> TabelasIrrf => Set<TabelaIrrf>();

    /// <summary>Tabelas RPPS municipais parametrizadas por competencia.</summary>
    public DbSet<TabelaRpps> TabelasRpps => Set<TabelaRpps>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecursosHumanosDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
