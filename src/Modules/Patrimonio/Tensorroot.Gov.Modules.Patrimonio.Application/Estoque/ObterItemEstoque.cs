using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Estoque;

/// <summary>Projeção de detalhe de um item de estoque para leitura.</summary>
/// <param name="Id">Identificador do item.</param>
/// <param name="Codigo">Código no catálogo.</param>
/// <param name="Descricao">Descrição.</param>
/// <param name="UnidadeMedida">Unidade de medida.</param>
/// <param name="Saldo">Saldo disponível.</param>
/// <param name="PontoPedido">Ponto de pedido.</param>
/// <param name="MetodoCusteio">Método de custeio (PEPS/Médio).</param>
/// <param name="CustoMedio">Custo médio ponderado atual.</param>
/// <param name="ClassificacaoAbc">Classe ABC.</param>
/// <param name="Situacao">Situação (Ativo/Inativo).</param>
public sealed record ItemEstoqueDetalhe(
    Guid Id,
    string Codigo,
    string Descricao,
    string UnidadeMedida,
    decimal Saldo,
    decimal PontoPedido,
    string MetodoCusteio,
    decimal CustoMedio,
    string ClassificacaoAbc,
    string Situacao);

/// <summary>Obtém o detalhe de um item de estoque (tenant-scoped).</summary>
/// <param name="ItemEstoqueId">Item de estoque.</param>
public sealed record ObterItemEstoqueQuery(Guid ItemEstoqueId) : IQuery<ItemEstoqueDetalhe?>;

/// <summary>Handler da consulta de detalhe de item de estoque.</summary>
public sealed class ObterItemEstoqueHandler(IItemEstoqueRepository itens)
    : IQueryHandler<ObterItemEstoqueQuery, ItemEstoqueDetalhe?>
{
    /// <inheritdoc />
    public async Task<ItemEstoqueDetalhe?> Handle(ObterItemEstoqueQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = await itens.ObterPorIdAsync(new ItemEstoqueId(request.ItemEstoqueId), cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            return null;
        }

        return new ItemEstoqueDetalhe(
            item.Id.Value,
            item.Codigo,
            item.Descricao,
            item.UnidadeMedida,
            item.Saldo.Quantidade,
            item.PontoPedido.Quantidade,
            item.MetodoCusteio.ToString(),
            item.CustoMedio.Valor,
            item.ClassificacaoAbc.ToString(),
            item.Situacao.ToString());
    }
}
