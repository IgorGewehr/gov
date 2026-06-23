using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Application.Common;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Requisicoes;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Requisicoes;

/// <summary>Item da lista de pedidos: projeção enxuta para a fila/tabela.</summary>
/// <param name="Id">Identificador do pedido.</param>
/// <param name="UnidadeId">UO consumidora.</param>
/// <param name="SetorSolicitante">Setor solicitante.</param>
/// <param name="Data">Data do pedido.</param>
/// <param name="Situacao">Situação na máquina de estados.</param>
/// <param name="TotalItens">Quantidade de linhas do pedido.</param>
public sealed record PedidoItemLista(
    Guid Id,
    Guid UnidadeId,
    string SetorSolicitante,
    DateOnly Data,
    string Situacao,
    int TotalItens);

/// <summary>
/// Lista/busca paginada de pedidos por situação e setor/UO (fila de aprovação/atendimento). Tenant-scoped
/// via Global Query Filter; read-only. Ordena por data decrescente.
/// </summary>
/// <param name="Situacao">Filtro opcional por situação.</param>
/// <param name="Setor">Filtro opcional por setor (case-insensível).</param>
/// <param name="UnidadeId">Filtro opcional por UO consumidora.</param>
/// <param name="Pagina">Página (base 1).</param>
/// <param name="Tamanho">Tamanho da página.</param>
public sealed record BuscarPedidosQuery(
    SituacaoPedido? Situacao,
    string? Setor,
    Guid? UnidadeId,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<PedidoItemLista>>;

/// <summary>Handler da busca paginada de pedidos de requisição.</summary>
public sealed class BuscarPedidosHandler(IPedidoRequisicaoRepository pedidos)
    : IQueryHandler<BuscarPedidosQuery, ResultadoPaginado<PedidoItemLista>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<PedidoItemLista>> Handle(
        BuscarPedidosQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var (itens, total) = await pedidos
            .BuscarAsync(request.Situacao, request.Setor, request.UnidadeId, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens
            .Select(pedido => new PedidoItemLista(
                pedido.Id.Value,
                pedido.UnidadeId,
                pedido.SetorSolicitante,
                pedido.Data,
                pedido.Situacao.ToString(),
                pedido.Itens.Count))
            .ToList();

        return new ResultadoPaginado<PedidoItemLista>(projetados, total, pagina, tamanho);
    }
}
