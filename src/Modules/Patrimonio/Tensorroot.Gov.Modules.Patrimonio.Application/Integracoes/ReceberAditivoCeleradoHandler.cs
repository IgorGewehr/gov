using MediatR;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Integracoes;

/// <summary>
/// Anti-Corruption Layer (entrada — Administração): consome <see cref="AditivoCeleradoIntegrationEvent"/>
/// e aplica o novo teto contratado à obra vinculada ao contrato (I-2). O limite legal do aditivo (25% /
/// 50% reforma — art. 125) é validado em Administração; Patrimônio apenas reaplica o teto recebido. Se não
/// houver obra para o contrato, o evento é ignorado (aquisição comum, não obra). Idempotente por <c>EventId</c>.
/// </summary>
public sealed class ReceberAditivoCeleradoHandler(
    IObraRepository obras,
    IUnitOfWork unitOfWork,
    ILogger<ReceberAditivoCeleradoHandler> logger)
    : INotificationHandler<AditivoCeleradoIntegrationEvent>
{
    /// <inheritdoc />
    public async Task Handle(AditivoCeleradoIntegrationEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var obra = await obras.ObterPorContratoAsync(notification.ContratoId, cancellationToken).ConfigureAwait(false);
        if (obra is null)
        {
            // Aditivo de contrato sem obra vinculada (aquisição comum): nada a fazer aqui.
            logger.LogInformation(
                "Aditivo {AditivoId} do contrato {ContratoId} sem obra vinculada — ignorado pelo Patrimonio.",
                notification.AditivoId,
                notification.ContratoId);
            return;
        }

        obra.AplicarAditivoValor(ValorMonetario.De(notification.ValorAtual));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Aditivo {AditivoId} aplicado à obra {ObraId}: novo teto contratado {ValorAtual}.",
            notification.AditivoId,
            obra.Id,
            notification.ValorAtual);
    }
}
