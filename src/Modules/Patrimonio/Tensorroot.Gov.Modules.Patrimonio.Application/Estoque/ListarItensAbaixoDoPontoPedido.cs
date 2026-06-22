using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Estoque;

/// <summary>Resumo de um item a repor (saldo abaixo ou igual ao ponto de pedido).</summary>
/// <param name="Id">Identificador do item.</param>
/// <param name="Codigo">Código no catálogo.</param>
/// <param name="Descricao">Descrição.</param>
/// <param name="Saldo">Saldo disponível.</param>
/// <param name="PontoPedido">Ponto de pedido.</param>
public sealed record ItemReposicaoResumo(
    Guid Id,
    string Codigo,
    string Descricao,
    decimal Saldo,
    decimal PontoPedido);

/// <summary>Lista os itens ativos cujo saldo está abaixo ou igual ao ponto de pedido (gatilho de reposição).</summary>
public sealed record ListarItensAbaixoDoPontoPedidoQuery() : IQuery<IReadOnlyList<ItemReposicaoResumo>>;

/// <summary>Handler da consulta de itens a repor.</summary>
public sealed class ListarItensAbaixoDoPontoPedidoHandler(IItemEstoqueRepository itens)
    : IQueryHandler<ListarItensAbaixoDoPontoPedidoQuery, IReadOnlyList<ItemReposicaoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ItemReposicaoResumo>> Handle(
        ListarItensAbaixoDoPontoPedidoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontrados = await itens.ListarAbaixoDoPontoPedidoAsync(cancellationToken).ConfigureAwait(false);

        return encontrados
            .Select(item => new ItemReposicaoResumo(
                item.Id.Value,
                item.Codigo,
                item.Descricao,
                item.Saldo.Quantidade,
                item.PontoPedido.Quantidade))
            .ToList();
    }
}
