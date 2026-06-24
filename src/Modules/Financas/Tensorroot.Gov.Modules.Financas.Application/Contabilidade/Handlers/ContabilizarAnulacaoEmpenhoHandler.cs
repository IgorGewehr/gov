using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Motor;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;
using Tensorroot.Gov.Modules.Financas.Domain.Events;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Handlers;

/// <summary>
/// Lançamento contábil automático da ANULAÇÃO de empenho: ao consumir <see cref="EmpenhoAnulado"/>,
/// aciona o roteiro <c>EVT-EMP-ANUL</c> (lançamento INVERSO ao do empenho — D Empenhado a Liquidar /
/// C Crédito Disponível), devolvendo o saldo ao controle orçamentário. Sem este handler, a conta
/// <c>6.2.2.1.3.01.00</c> nunca é creditada de volta e o balancete superavalia a despesa empenhada
/// permanentemente (MSC/remessa SIAPC ao TCE-RS divergem da execução real).
/// </summary>
/// <remarks>
/// Idempotente por <c>EmpenhoAnulado.EventoId</c> (FATO de anulação), permitindo anulações parciais
/// sucessivas do mesmo empenho — cada uma gera seu próprio lançamento inverso. Reprodutível: a
/// competência usa a <c>DataEmpenho</c> (exercício do empenho), nunca o relógio, garantindo que o
/// estorno reverta exatamente as contas do exercício originalmente onerado.
/// </remarks>
public sealed class ContabilizarAnulacaoEmpenhoHandler(
    MotorContabil motor,
    IEmpenhoRepository empenhos,
    IUnitOfWork unitOfWork) : INotificationHandler<EmpenhoAnulado>
{
    /// <inheritdoc />
    public async Task Handle(EmpenhoAnulado notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var empenho = await empenhos.ObterPorIdAsync(notification.EmpenhoId, cancellationToken).ConfigureAwait(false);
        if (empenho is null)
        {
            return;
        }

        var gerados = await motor.ContabilizarAsync(
            FatoContabil.EmpenhoAnulado,
            ValorMonetario.De(notification.Valor),
            empenho.DataEmpenho,
            notification.EventoId,
            cancellationToken).ConfigureAwait(false);

        if (gerados > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
