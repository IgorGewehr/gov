using MediatR;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.Modules.Protocolo.Application.Documentos;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada): consome <see cref="SolicitarAssinaturaRequested"/> publicado por
/// outro modulo e dispara a assinatura local por criticidade (rejeita nivel insuficiente).
/// Idempotente por <c>EventId</c> (deduplicacao no Inbox).
/// </summary>
public sealed class ReceberSolicitarAssinaturaRequestedHandler(
    ISender sender,
    ILogger<ReceberSolicitarAssinaturaRequestedHandler> logger)
    : INotificationHandler<SolicitarAssinaturaRequested>
{
    /// <inheritdoc />
    public async Task Handle(SolicitarAssinaturaRequested notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        logger.LogInformation(
            "Assinatura solicitada por outro modulo para tenant {TenantId}: documento {DocumentoId}, nivel {Tipo}.",
            notification.TenantId,
            notification.DocumentoId,
            notification.Tipo);

        await sender.Send(
            new AssinarDocumentoCommand(
                notification.DocumentoId,
                notification.SignatarioId,
                notification.Tipo),
            cancellationToken).ConfigureAwait(false);
    }
}
