using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;

/// <summary>
/// Interceptor que, a cada SaveChanges, gera registros imutáveis de <see cref="AuditTrail"/>
/// (valores antes/depois em JSON, usuário, IP e timestamp) para o Tribunal de Contas.
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is not null)
        {
            AddAuditEntries(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is not null)
        {
            AddAuditEntries(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditEntries(DbContext context)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var entries = new List<AuditTrail>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditTrail or OutboxMessage)
            {
                continue;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var oldValues = new Dictionary<string, object?>(StringComparer.Ordinal);
            var newValues = new Dictionary<string, object?>(StringComparer.Ordinal);
            var affected = new List<string>();
            object? primaryKey = null;

            // Colunas sensiveis (material cifrado) que NUNCA podem ser serializadas na trilha — o
            // valor e substituido por um marcador opaco (A1-DESIGN §2/§7 risco 6, CLAUDE.md §6).
            var redactadas = entry.Entity is IHasRedactedAuditFields sensivel
                ? sensivel.ColunasAuditoriaRedactadas
                : null;

            foreach (var property in entry.Properties)
            {
                var name = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    primaryKey = property.CurrentValue;
                }

                var redactar = redactadas is not null && redactadas.Contains(name);

                switch (entry.State)
                {
                    case EntityState.Added:
                        newValues[name] = redactar ? IHasRedactedAuditFields.RedactionMarker : property.CurrentValue;
                        break;
                    case EntityState.Deleted:
                        oldValues[name] = redactar ? IHasRedactedAuditFields.RedactionMarker : property.OriginalValue;
                        break;
                    case EntityState.Modified when property.IsModified:
                        oldValues[name] = redactar ? IHasRedactedAuditFields.RedactionMarker : property.OriginalValue;
                        newValues[name] = redactar ? IHasRedactedAuditFields.RedactionMarker : property.CurrentValue;
                        affected.Add(name);
                        break;
                    default:
                        break;
                }
            }

            var tenantId = entry.Entity is IMustHaveTenant tenant ? tenant.TenantId : Guid.Empty;

            entries.Add(new AuditTrail
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = primaryKey is null ? null : Convert.ToString(primaryKey, CultureInfo.InvariantCulture),
                Action = entry.State.ToString(),
                OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
                AffectedColumns = affected.Count > 0 ? JsonSerializer.Serialize(affected) : null,
                UserId = currentUser.UserId,
                IpAddress = currentUser.IpAddress,
                TimestampUtc = nowUtc,
            });
        }

        if (entries.Count > 0)
        {
            context.Set<AuditTrail>().AddRange(entries);
        }
    }
}
