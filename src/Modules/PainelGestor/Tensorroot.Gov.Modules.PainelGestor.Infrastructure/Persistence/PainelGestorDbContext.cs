using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Indicadores;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Ingestao;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Limites;

namespace Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo PainelGestor (schema isolado "painelgestor"), herdando Outbox, Audit Trail e o
/// Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Módulo READ-ONLY: não publica eventos
/// próprios (sem agregados de escrita de negócio) — só materializa read models a partir dos Integration
/// Events dos módulos-fonte. Implementa <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class PainelGestorDbContext(
    DbContextOptions<PainelGestorDbContext> options,
    ITenantContext tenantContext,
    ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "painelgestor";

    /// <summary>Read model consolidado por exercício (alimentado por Integration Events).</summary>
    public DbSet<IndicadorMunicipioSnapshot> Indicadores => Set<IndicadorMunicipioSnapshot>();

    /// <summary>Ledger de idempotência da ingestão (EventId já consumido por tenant — I-13).</summary>
    public DbSet<EventoIngerido> EventosIngeridos => Set<EventoIngerido>();

    /// <summary>Limites de pessoal (LRF) versionados por tenant+vigência (nunca hardcoded — §7).</summary>
    public DbSet<ParametroLimitePessoal> ParametrosLimitePessoal => Set<ParametroLimitePessoal>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PainelGestorDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
