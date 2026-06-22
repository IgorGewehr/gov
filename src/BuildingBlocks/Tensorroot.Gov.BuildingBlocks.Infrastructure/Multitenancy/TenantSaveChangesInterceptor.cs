using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;

/// <summary>
/// Interceptor que carimba o <c>TenantId</c> em entidades novas e BLOQUEIA qualquer
/// gravação cross-tenant (vazamento entre inquilinos), lançando exceção.
/// </summary>
public sealed class TenantSaveChangesInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ApplyTenant(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ApplyTenant(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyTenant(DbContext? context)
    {
        if (context is null || !tenantContext.HasTenant)
        {
            return;
        }

        var tenantId = tenantContext.TenantId;

        foreach (var entry in context.ChangeTracker.Entries<IMustHaveTenant>())
        {
            var tenantProperty = entry.Property(nameof(IMustHaveTenant.TenantId));
            switch (entry.State)
            {
                case EntityState.Added:
                    tenantProperty.CurrentValue = tenantId;
                    break;
                case EntityState.Modified or EntityState.Deleted:
                    if (!Equals(tenantProperty.CurrentValue, tenantId))
                    {
                        throw new InvalidOperationException(
                            $"Gravação cross-tenant bloqueada na entidade '{entry.Metadata.ClrType.Name}'.");
                    }

                    break;
                default:
                    break;
            }
        }
    }
}
