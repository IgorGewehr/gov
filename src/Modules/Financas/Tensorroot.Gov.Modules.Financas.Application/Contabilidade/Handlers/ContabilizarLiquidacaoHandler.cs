using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Motor;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;
using Tensorroot.Gov.Modules.Financas.Domain.Events;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Handlers;

/// <summary>
/// Lançamento contábil automático da liquidação: ao consumir <see cref="DespesaLiquidada"/>, gera DOIS
/// lançamentos homogêneos — orçamentário (D Empenhado a Liquidar / C Liquidado a Pagar) e patrimonial
/// (D VPD / C Fornecedores). Idempotente por <c>LiquidacaoId</c>.
/// </summary>
public sealed class ContabilizarLiquidacaoHandler(
    MotorContabil motor,
    ILiquidacaoRepository liquidacoes,
    IUnitOfWork unitOfWork) : INotificationHandler<DespesaLiquidada>
{
    /// <inheritdoc />
    public async Task Handle(DespesaLiquidada notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var liquidacao = await liquidacoes.ObterPorIdAsync(notification.LiquidacaoId, cancellationToken).ConfigureAwait(false);
        if (liquidacao is null)
        {
            return;
        }

        var gerados = await motor.ContabilizarAsync(
            FatoContabil.DespesaLiquidada,
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
