using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Interceptor que materializa, na MESMA transação do SaveChanges, os eventos de domínio
/// acumulados nas entidades como <see cref="OutboxMessage"/> (Outbox Pattern) — garantindo
/// consistência transacional entre o estado e a publicação posterior dos eventos.
/// </summary>
public sealed class ConvertDomainEventsToOutboxInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is not null)
        {
            Converter(eventData.Context);
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
            Converter(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Converter(DbContext context)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var portadores = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .ToList();

        var mensagens = new List<OutboxMessage>();
        foreach (var entry in portadores)
        {
            var tenantId = entry.Entity is IMustHaveTenant comTenant ? comTenant.TenantId : Guid.Empty;

            foreach (var evento in entry.Entity.DomainEvents)
            {
                mensagens.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Type = evento.GetType().AssemblyQualifiedName ?? evento.GetType().FullName!,
                    Content = JsonSerializer.Serialize(evento, evento.GetType()),
                    OccurredOnUtc = nowUtc,
                });
            }

            entry.Entity.ClearDomainEvents();
        }

        if (mensagens.Count > 0)
        {
            context.Set<OutboxMessage>().AddRange(mensagens);
        }
    }
}
