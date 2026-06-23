using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;

namespace Tensorroot.Gov.ApiHost.Outbox;

/// <summary>
/// Despachante de Outbox que ISOLA cada CONSUMIDOR de uma mensagem em seu PRÓPRIO escopo de DI.
/// <para>
/// Para cada handler registrado para o tipo do evento (lookup no <see cref="OutboxHandlerRegistry"/>),
/// abre um escopo novo, carimba o <see cref="TenantOverride"/> com o tenant da mensagem (banco dedicado
/// + Global Query Filters) e resolve+invoca APENAS aquele handler ali. Assim, cada consumidor resolve
/// SÓ o <c>ModuleDbContext</c> do seu módulo no escopo — nunca dois contextos divergentes no mesmo
/// escopo — eliminando o gatilho da guarda H5 (<c>ScopeDbContextHolder</c>).
/// </para>
/// <para>
/// CRÍTICO (achado de runtime M8): um único integration event pode ter consumidores em MÓDULOS
/// DISTINTOS (ex.: <c>ReceitaArrecadadaIntegrationEvent</c> → Finanças + Painel do Gestor). Publicar
/// todos via um único <c>IPublisher.Publish</c> — ou mesmo só ENUMERAR o
/// <c>IEnumerable&lt;INotificationHandler&gt;</c> num escopo — construiria <c>FinancasDbContext</c> e
/// <c>PainelGestorDbContext</c> no mesmo escopo, acionando H5 e deixando a mensagem presa (AttemptCount
/// subindo). Por isso o isolamento é POR HANDLER (registry de tipos), nunca por mensagem.
/// </para>
/// <para>
/// Vive no ApiHost porque depende de <see cref="IServiceScopeFactory"/> + <see cref="TenantOverride"/>
/// (camada superior). O <c>OutboxPublisher</c> (BuildingBlocks) só conhece a abstração
/// <see cref="IOutboxMessageDispatcher"/>, preservando o layering.
/// </para>
/// </summary>
internal sealed class ScopedOutboxMessageDispatcher(
    IServiceScopeFactory scopeFactory,
    OutboxHandlerRegistry registry) : IOutboxMessageDispatcher
{
    /// <inheritdoc />
    public async Task DespacharAsync(object evento, Guid tenantId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(evento);

        var tipoEvento = evento.GetType();
        var handlerInterface = typeof(INotificationHandler<>).MakeGenericType(tipoEvento);
        var handleMethod = handlerInterface.GetMethod(nameof(INotificationHandler<INotification>.Handle))!;

        foreach (var tipoHandler in registry.HandlersDe(tipoEvento))
        {
            // Um escopo DEDICADO por handler: garante UM ModuleDbContext por escopo, mesmo quando o mesmo
            // evento é consumido por módulos distintos. A idempotência at-least-once já existente em cada
            // consumidor torna seguro o reprocessamento após uma falha parcial.
            await using var escopo = scopeFactory.CreateAsyncScope();
            escopo.ServiceProvider.GetRequiredService<TenantOverride>().TenantId = tenantId;

            var handler = ActivatorUtilities.CreateInstance(escopo.ServiceProvider, tipoHandler);
            var tarefa = (Task)handleMethod.Invoke(handler, [evento, cancellationToken])!;
            await tarefa.ConfigureAwait(false);
        }
    }
}
