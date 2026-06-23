using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Cidadao.Domain.Contas;

namespace Tensorroot.Gov.Modules.Cidadao.Infrastructure.Persistence;

/// <summary>
/// DbContext do modulo Cidadao (schema isolado "cidadao"), no banco DEDICADO do tenant. Herda Outbox,
/// Audit Trail e o Global Query Filter por tenant de <see cref="ModuleDbContext"/> e implementa
/// <see cref="IUnitOfWork"/>. SEGURANCA CRITICA: as senhas vivem aqui apenas como hash (BCrypt), nunca
/// em claro. O CTOR e inalterado (mesma assinatura dos demais modulos — provisionamento/UoW/holder).
/// </summary>
public sealed class CidadaoDbContext(DbContextOptions<CidadaoDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "cidadao";

    /// <summary>Contas-cidadao (principais externos do Portal do Cidadao) do tenant.</summary>
    public DbSet<CidadaoConta> Contas => Set<CidadaoConta>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CidadaoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
