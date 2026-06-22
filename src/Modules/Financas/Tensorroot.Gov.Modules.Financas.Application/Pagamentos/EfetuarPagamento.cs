using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Pagamentos;

namespace Tensorroot.Gov.Modules.Financas.Application.Pagamentos;

/// <summary>Efetua o pagamento de uma ordem (3º estágio — Lei 4.320/64, art. 64).</summary>
/// <param name="OrdemDePagamentoId">Identificador da ordem.</param>
public sealed record EfetuarPagamentoCommand(Guid OrdemDePagamentoId) : ICommand;

/// <summary>Cancela uma ordem de pagamento ainda não efetuada.</summary>
/// <param name="OrdemDePagamentoId">Identificador da ordem.</param>
public sealed record CancelarOrdemDePagamentoCommand(Guid OrdemDePagamentoId) : ICommand;

/// <summary>
/// Handler da efetivação do pagamento. Para cada item aplica o invariante de saldo na
/// liquidação (<see cref="Domain.Liquidacoes.Liquidacao.RegistrarPagamento"/>) e no
/// empenho (<see cref="Domain.Empenhos.Empenho.RegistrarPagamento"/>) na mesma transação.
/// </summary>
public sealed class EfetuarPagamentoHandler(
    IOrdemDePagamentoRepository ordens,
    ILiquidacaoRepository liquidacoes,
    IEmpenhoRepository empenhos,
    IUnitOfWork unitOfWork) : ICommandHandler<EfetuarPagamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(EfetuarPagamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ordem = await ordens.ObterPorIdAsync(new OrdemDePagamentoId(request.OrdemDePagamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ordem de pagamento nao encontrada.");

        foreach (var item in ordem.Itens)
        {
            var liquidacao = await liquidacoes.ObterPorIdAsync(item.LiquidacaoId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Liquidacao {item.LiquidacaoId} nao encontrada.");

            // Invariante (art. 62): nao pagar acima do liquidado — na liquidacao e no empenho.
            liquidacao.RegistrarPagamento(item.Valor);

            var empenho = await empenhos.ObterPorIdAsync(liquidacao.EmpenhoId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Empenho da liquidacao nao encontrado.");

            empenho.RegistrarPagamento(item.Valor);
        }

        ordem.Efetuar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler do cancelamento de ordem de pagamento.</summary>
public sealed class CancelarOrdemDePagamentoHandler(IOrdemDePagamentoRepository ordens, IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarOrdemDePagamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarOrdemDePagamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ordem = await ordens.ObterPorIdAsync(new OrdemDePagamentoId(request.OrdemDePagamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Ordem de pagamento nao encontrada.");

        ordem.Cancelar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
