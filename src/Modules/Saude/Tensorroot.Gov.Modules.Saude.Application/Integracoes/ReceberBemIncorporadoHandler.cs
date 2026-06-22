using MediatR;
using Microsoft.Extensions.Logging;

namespace Tensorroot.Gov.Modules.Saude.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada — Patrimonio): consome <see cref="BemIncorporado"/>
/// para vincular o equipamento incorporado a UBS/estabelecimento de saude. Nao altera o estado do
/// agregado Atendimento (consumo de contexto do modulo). Idempotente por <c>EventId</c> (deduplicacao no Inbox).
/// </summary>
public sealed class ReceberBemIncorporadoHandler(ILogger<ReceberBemIncorporadoHandler> logger)
    : INotificationHandler<BemIncorporado>
{
    /// <inheritdoc />
    public Task Handle(BemIncorporado notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        logger.LogInformation(
            "Bem incorporado recebido do Patrimonio para tenant {TenantId}: bem {BemPatrimonialId}.",
            notification.TenantId,
            notification.BemPatrimonialId);

        return Task.CompletedTask;
    }
}
