using MediatR;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Application.Processos;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada — modulos originadores): consome <see cref="AutuarProcessoRequested"/>
/// (Licitacoes, RH/Ferias, Licencas) e autua o processo preservando a origem, respondendo com
/// <c>ProcessoAutuadoIntegrationEvent</c> (NUP de volta ao originador). Idempotente por <c>EventId</c>
/// (deduplicacao no Inbox).
/// </summary>
public sealed class ReceberAutuarProcessoRequestedHandler(ISender sender)
    : INotificationHandler<AutuarProcessoRequested>
{
    /// <inheritdoc />
    public async Task Handle(AutuarProcessoRequested notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var comando = new AutuarProcessoCommand(
            Guid.Empty,
            notification.Classificacao,
            NivelDeAcesso.Restrito,
            notification.OrigemModulo,
            notification.OrigemId);

        await sender.Send(comando, cancellationToken).ConfigureAwait(false);
    }
}
