using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Motor;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;
using Tensorroot.Gov.Modules.Financas.Domain.Events;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Handlers;

/// <summary>
/// Lançamento contábil automático do empenho: ao consumir <see cref="EmpenhoEmitido"/> (via Outbox),
/// resolve o roteiro vigente e persiste o lançamento orçamentário (D Crédito Disponível / C Empenhado
/// a Liquidar) na mesma unidade de trabalho do handler. Idempotente por <c>EmpenhoId</c>.
/// </summary>
public sealed class ContabilizarEmpenhoHandler(
    MotorContabil motor,
    IEmpenhoRepository empenhos,
    IUnitOfWork unitOfWork) : INotificationHandler<EmpenhoEmitido>
{
    /// <inheritdoc />
    public async Task Handle(EmpenhoEmitido notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var empenho = await empenhos.ObterPorIdAsync(notification.EmpenhoId, cancellationToken).ConfigureAwait(false);
        if (empenho is null)
        {
            return;
        }

        var gerados = await motor.ContabilizarAsync(
            FatoContabil.EmpenhoEmitido,
            ValorMonetario.De(notification.Valor),
            empenho.DataEmpenho,
            notification.EmpenhoId.Value,
            cancellationToken).ConfigureAwait(false);

        if (gerados > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
