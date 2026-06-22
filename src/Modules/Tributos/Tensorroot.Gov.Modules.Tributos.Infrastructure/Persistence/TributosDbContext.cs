using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.Nfse;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Tributos (schema isolado "tributos"), herdando Outbox, Audit Trail
/// e o Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Implementa <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class TributosDbContext(DbContextOptions<TributosDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "tributos";

    /// <summary>Contribuintes.</summary>
    public DbSet<Contribuinte> Contribuintes => Set<Contribuinte>();

    /// <summary>Lançamentos tributários.</summary>
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();

    /// <summary>Dívidas ativas.</summary>
    public DbSet<DividaAtiva> DividasAtivas => Set<DividaAtiva>();

    /// <summary>NFS-e sincronizadas do Ambiente Nacional (read model fiscal — ADR-0003).</summary>
    public DbSet<NotaFiscalServico> NotasFiscaisServico => Set<NotaFiscalServico>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TributosDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
