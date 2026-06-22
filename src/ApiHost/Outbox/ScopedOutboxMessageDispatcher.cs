using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;

namespace Tensorroot.Gov.ApiHost.Outbox;

/// <summary>
/// Despachante de Outbox que ISOLA cada mensagem em seu PRÓPRIO escopo de DI.
/// <para>
/// Para cada mensagem, abre um escopo novo, carimba o <see cref="TenantOverride"/> com o tenant da
/// mensagem (para o banco dedicado e os Global Query Filters) e publica via MediatR ali. Assim, os
/// handlers de cada módulo resolvem APENAS o <c>ModuleDbContext</c> do seu próprio módulo no escopo —
/// nunca dois contextos divergentes no mesmo escopo — eliminando o gatilho da guarda H5
/// (<c>ScopeDbContextHolder</c>) quando uma drenagem cruza módulos (ex.: domain event de Finanças +
/// integration event consumido por Administração).
/// </para>
/// <para>
/// Vive no ApiHost porque depende de <see cref="IServiceScopeFactory"/> + <see cref="TenantOverride"/>
/// (camada superior). O <c>OutboxPublisher</c> (BuildingBlocks) só conhece a abstração
/// <see cref="IOutboxMessageDispatcher"/>, preservando o layering.
/// </para>
/// </summary>
internal sealed class ScopedOutboxMessageDispatcher(IServiceScopeFactory scopeFactory) : IOutboxMessageDispatcher
{
    /// <inheritdoc />
    public async Task DespacharAsync(object evento, Guid tenantId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(evento);

        await using var escopo = scopeFactory.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<TenantOverride>().TenantId = tenantId;

        var publisher = escopo.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
