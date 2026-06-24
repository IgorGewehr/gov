using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Motor;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;
using Tensorroot.Gov.Modules.Financas.Domain.Events;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Handlers;

/// <summary>
/// Lançamento contábil automático do ESTORNO de liquidação: ao consumir <see cref="LiquidacaoEstornada"/>,
/// aciona o roteiro <c>EVT-LIQ-EST</c> (lançamento INVERSO ao da liquidação, em AMBOS os blocos —
/// orçamentário D Liquidado a Pagar / C Empenhado a Liquidar, e patrimonial D Fornecedores / C VPD).
/// Sem este handler, as contas <c>6.2.2.1.3.03.00</c>/Fornecedores ficam infladas indefinidamente e o
/// balancete não volta a bater após o estorno.
/// </summary>
/// <remarks>
/// Idempotente por <c>LiquidacaoId</c> — uma liquidação só pode ser estornada uma vez (o agregado
/// barra estorno repetido), então a origem da liquidação identifica o fato sem ambiguidade.
/// Reprodutível: a competência usa a <c>DataLiquidacao</c>, nunca o relógio.
/// </remarks>
public sealed class ContabilizarEstornoLiquidacaoHandler(
    MotorContabil motor,
    ILiquidacaoRepository liquidacoes,
    IUnitOfWork unitOfWork) : INotificationHandler<LiquidacaoEstornada>
{
    /// <inheritdoc />
    public async Task Handle(LiquidacaoEstornada notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var liquidacao = await liquidacoes.ObterPorIdAsync(notification.LiquidacaoId, cancellationToken).ConfigureAwait(false);
        if (liquidacao is null)
        {
            return;
        }

        var gerados = await motor.ContabilizarAsync(
            FatoContabil.LiquidacaoEstornada,
            ValorMonetario.De(notification.Valor),
            liquidacao.DataLiquidacao,
            notification.LiquidacaoId.Value,
            cancellationToken).ConfigureAwait(false);

        if (gerados > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
