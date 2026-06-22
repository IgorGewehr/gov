using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Transparencia (schema isolado "transparencia"), herdando Outbox, Audit Trail
/// e o Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Implementa <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class TransparenciaDbContext(DbContextOptions<TransparenciaDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "transparencia";

    /// <summary>Remessas ao TCE-RS (SIAPC/PAD) — papel CONSUMIDOR.</summary>
    public DbSet<RemessaTce> RemessasTce => Set<RemessaTce>();

    /// <summary>Declaracoes fiscais (MSC/RREO/RGF/DCA) transmitidas ao SICONFI.</summary>
    public DbSet<DeclaracaoFiscal> DeclaracoesFiscais => Set<DeclaracaoFiscal>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransparenciaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
