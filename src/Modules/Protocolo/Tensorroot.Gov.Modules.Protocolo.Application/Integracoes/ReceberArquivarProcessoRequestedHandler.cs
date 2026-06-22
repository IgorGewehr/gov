using MediatR;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Application.Processos;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada — outros modulos): consome <see cref="ArquivarProcessoRequested"/>,
/// resolve o processo pelo NUP e arquiva o processo originado pelo modulo. Idempotente por <c>EventId</c>
/// (deduplicacao no Inbox).
/// </summary>
public sealed class ReceberArquivarProcessoRequestedHandler(IProcessoRepository processos, ISender sender)
    : INotificationHandler<ArquivarProcessoRequested>
{
    /// <inheritdoc />
    public async Task Handle(ArquivarProcessoRequested notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var processo = await processos.ObterPorNupAsync(new Nup(notification.Nup), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Processo nao encontrado.");

        var comando = new ArquivarProcessoCommand(processo.Id.Value, notification.Motivo);
        await sender.Send(comando, cancellationToken).ConfigureAwait(false);
    }
}
