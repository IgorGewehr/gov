using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Handlers;

/// <summary>
/// Atualiza a projeção do Balancete ao consumir <see cref="LancamentoContabilRegistrado"/>: para cada
/// partida, incrementa débitos/créditos da linha (conta, exercício, mês) e recalcula o saldo conforme
/// a natureza do saldo. Base da geração da MSC.
/// </summary>
public sealed class ProjetarBalanceteHandler(
    ILancamentoContabilRepository lancamentos,
    IContaContabilRepository contas,
    IBalanceteProjection balancete,
    IUnitOfWork unitOfWork) : INotificationHandler<LancamentoContabilRegistrado>
{
    /// <inheritdoc />
    public async Task Handle(LancamentoContabilRegistrado notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var lancamento = await lancamentos.ObterPorIdAsync(notification.LancamentoId, cancellationToken).ConfigureAwait(false);
        if (lancamento is null)
        {
            return;
        }

        foreach (var partida in lancamento.Partidas)
        {
            var conta = await contas.ObterPorIdAsync(partida.ContaId, cancellationToken).ConfigureAwait(false);
            if (conta is null)
            {
                continue;
            }

            var linha = await balancete.ObterOuCriarLinhaAsync(
                conta,
                notification.Exercicio,
                notification.PeriodoMes,
                cancellationToken).ConfigureAwait(false);

            if (partida.Lado == LadoPartida.Debito)
            {
                linha.TotalDebitos += partida.Valor.Valor;
            }
            else
            {
                linha.TotalCreditos += partida.Valor.Valor;
            }

            linha.RecalcularSaldo();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
