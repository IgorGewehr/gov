using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Patrimonio (schema isolado "patrimonio"), herdando Outbox, Audit Trail
/// e o Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Implementa <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class PatrimonioDbContext(DbContextOptions<PatrimonioDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "patrimonio";

    /// <summary>Bens patrimoniais (móveis e imóveis).</summary>
    public DbSet<BemPatrimonial> Bens => Set<BemPatrimonial>();

    /// <summary>Veículos da frota pública.</summary>
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();

    /// <summary>Itens de almoxarifado (estoque).</summary>
    public DbSet<ItemEstoque> ItensEstoque => Set<ItemEstoque>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatrimonioDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
