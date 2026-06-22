using MediatR;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.Modules.Protocolo.Application.Documentos;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada): consome <see cref="JuntarDocumentoRequested"/> publicado por um
/// modulo originador (Licitacoes/RH/Licencas) e dispara a juntada local do documento ao processo
/// (PDF/A + hash). Idempotente por <c>EventId</c> (deduplicacao no Inbox).
/// </summary>
public sealed class ReceberJuntarDocumentoRequestedHandler(
    ISender sender,
    ILogger<ReceberJuntarDocumentoRequestedHandler> logger)
    : INotificationHandler<JuntarDocumentoRequested>
{
    /// <inheritdoc />
    public async Task Handle(JuntarDocumentoRequested notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        logger.LogInformation(
            "Juntada solicitada por modulo originador para tenant {TenantId}: processo {ProcessoId}.",
            notification.TenantId,
            notification.ProcessoId);

        await sender.Send(
            new JuntarDocumentoCommand(
                notification.ProcessoId,
                notification.Hash,
                notification.Criticidade,
                notification.NivelAcesso,
                notification.FormatoPdfA),
            cancellationToken).ConfigureAwait(false);
    }
}
