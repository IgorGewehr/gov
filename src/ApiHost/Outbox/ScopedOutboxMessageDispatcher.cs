using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Inbox;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;
using Tensorroot.Gov.SharedKernel;

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

        // EventId estável do evento (chave do Inbox). Integration Events expõem EventId; um evento sem
        // EventId (raríssimo: domain event que vazou ao Outbox) cai no caminho SEM Inbox (Guid.Empty).
        var eventId = (evento as IIntegrationEvent)?.EventId ?? Guid.Empty;
        var eventType = tipoEvento.AssemblyQualifiedName;

        foreach (var tipoHandler in registry.HandlersDe(tipoEvento))
        {
            // Um escopo DEDICADO por handler: garante UM ModuleDbContext por escopo, mesmo quando o mesmo
            // evento é consumido por módulos distintos. A idempotência at-least-once já existente em cada
            // consumidor torna seguro o reprocessamento após uma falha parcial.
            await using var escopo = scopeFactory.CreateAsyncScope();
            escopo.ServiceProvider.GetRequiredService<TenantOverride>().TenantId = tenantId;

            var handlerNome = tipoHandler.FullName ?? tipoHandler.Name;
            var inbox = escopo.ServiceProvider.GetRequiredService<IInboxGuard>();

            // PRÉ-CHECK do Inbox: se este handler/tenant JÁ consumiu o evento (redrenagem após falha
            // parcial/reinício/backoff), PULA — não reaplica o efeito colateral. O contexto do módulo
            // ainda não foi resolvido aqui, então o pré-check só acerta quando há contexto vivo no
            // escopo; a guarda DURA é a unicidade no commit (capturada abaixo).
            if (eventId != Guid.Empty
                && await inbox.JaProcessadoAsync(eventId, handlerNome, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            var handler = ActivatorUtilities.CreateInstance(escopo.ServiceProvider, tipoHandler);
            var tarefa = (Task)handleMethod.Invoke(handler, [evento, cancellationToken])!;
            await tarefa.ConfigureAwait(false);

            await SelarConsumoAsync(escopo.ServiceProvider, inbox, eventId, handlerNome, eventType, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sela o consumo no Inbox e confirma — ATÔMICO com o efeito do handler (mesmo ModuleDbContext do
    /// escopo). Se o handler tocou seu contexto, a linha do Inbox + as mutações confirmam juntas. Se a
    /// trinca já existir (corrida com outra drenagem), o UNIQUE no commit lança e o consumo é tratado
    /// como duplicado (no-op idempotente) em vez de aplicar o efeito duas vezes.
    /// </summary>
    private static async Task SelarConsumoAsync(
        IServiceProvider serviceProvider,
        IInboxGuard inbox,
        Guid eventId,
        string handlerNome,
        string? eventType,
        CancellationToken cancellationToken)
    {
        var holder = serviceProvider.GetRequiredService<ScopeDbContextHolder>();
        var contexto = holder.Atual;
        if (eventId == Guid.Empty || contexto is null)
        {
            // Sem EventId (não é Integration Event) ou handler que não tocou contexto de módulo: nada a
            // selar de forma transacional. A idempotência at-least-once do handler segue valendo.
            return;
        }

        inbox.RegistrarProcessado(eventId, handlerNome, eventType);

        try
        {
            await contexto.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // C# NÃO permite `await` no filtro de catch (CS7094): a checagem da corrida é assíncrona (lê o
            // Inbox), então ela vive no CORPO do catch. Se a trinca JÁ está selada por outra transação, foi
            // o UNIQUE (EventId, Handler, TenantId) rejeitando nossa inserção — consumo duplicado, no-op
            // idempotente. Caso contrário, a falha não é de corrida do Inbox: REPROPAGA para o despachante
            // (a mensagem volta ao Outbox com backoff, sem mascarar o erro real).
            if (!await ConsumoJaSeladoAsync(contexto, eventId, handlerNome, cancellationToken).ConfigureAwait(false))
            {
                throw;
            }

            // Corrida: outra drenagem selou a MESMA trinca primeiro. O efeito do handler que NÃO foi
            // confirmado será reentregue numa próxima drenagem e, então, pulado pelo pré-check.
        }
    }

    /// <summary>Confirma que a trinca já está selada por outra transação (distingue corrida de Inbox de outra violação).</summary>
    private static async Task<bool> ConsumoJaSeladoAsync(
        ModuleDbContext contexto,
        Guid eventId,
        string handlerNome,
        CancellationToken cancellationToken)
    {
        return await contexto.Set<InboxMessage>()
            .AsNoTracking()
            .AnyAsync(inbox => inbox.EventId == eventId && inbox.Handler == handlerNome, cancellationToken)
            .ConfigureAwait(false);
    }
}
