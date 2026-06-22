using MediatR;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Application.Processos;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada — outros modulos): consome <see cref="TramitarProcessoRequested"/>,
/// resolve o processo pelo NUP e tramita-o ao setor solicitado. Idempotente por <c>EventId</c>
/// (deduplicacao no Inbox).
/// </summary>
public sealed class ReceberTramitarProcessoRequestedHandler(IProcessoRepository processos, ISender sender)
    : INotificationHandler<TramitarProcessoRequested>
{
    /// <inheritdoc />
    public async Task Handle(TramitarProcessoRequested notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var processo = await processos.ObterPorNupAsync(new Nup(notification.Nup), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Processo nao encontrado.");

        var comando = new TramitarProcessoCommand(processo.Id.Value, notification.SetorDestinoId, null);
        await sender.Send(comando, cancellationToken).ConfigureAwait(false);
    }
}
