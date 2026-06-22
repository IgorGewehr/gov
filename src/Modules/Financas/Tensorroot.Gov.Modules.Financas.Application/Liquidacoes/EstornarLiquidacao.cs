using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;

namespace Tensorroot.Gov.Modules.Financas.Application.Liquidacoes;

/// <summary>Estorna uma liquidação (somente se nada foi pago).</summary>
/// <param name="LiquidacaoId">Identificador da liquidação.</param>
public sealed record EstornarLiquidacaoCommand(Guid LiquidacaoId) : ICommand;

/// <summary>
/// Handler do estorno de liquidação. Estorna a liquidação e devolve o saldo a liquidar
/// no empenho (<see cref="Empenho.EstornarLiquidacao"/>) na mesma transação.
/// </summary>
public sealed class EstornarLiquidacaoHandler(
    ILiquidacaoRepository liquidacoes,
    IEmpenhoRepository empenhos,
    IUnitOfWork unitOfWork) : ICommandHandler<EstornarLiquidacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(EstornarLiquidacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var liquidacao = await liquidacoes.ObterPorIdAsync(new LiquidacaoId(request.LiquidacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Liquidacao nao encontrada.");

        var empenho = await empenhos.ObterPorIdAsync(liquidacao.EmpenhoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empenho da liquidacao nao encontrado.");

        liquidacao.Estornar();
        empenho.EstornarLiquidacao(liquidacao.Valor);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
