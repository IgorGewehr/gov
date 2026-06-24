using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;
using Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;

namespace Tensorroot.Gov.Modules.Convenios.Infrastructure.Persistence;

/// <summary>
/// DbContext do modulo Convenios (schema isolado "convenios"), herdando Outbox, Audit Trail e o Global Query
/// Filter por tenant de <see cref="ModuleDbContext"/>. Hospeda os DOIS agregados-raiz dos dois fluxos
/// separados: <see cref="ConvenioRecebido"/> (federais recebidos) e <see cref="ParceriaOsc"/> (MROSC).
/// Implementa <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class ConveniosDbContext(
    DbContextOptions<ConveniosDbContext> options,
    ITenantContext tenantContext,
    ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "convenios";

    /// <summary>Convenios federais RECEBIDOS (fluxo A — Dec. 11.531/2023).</summary>
    public DbSet<ConvenioRecebido> ConveniosRecebidos => Set<ConvenioRecebido>();

    /// <summary>Parcerias-saida OSC (fluxo B — MROSC Lei 13.019/2014).</summary>
    public DbSet<ParceriaOsc> ParceriasOsc => Set<ParceriaOsc>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConveniosDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
