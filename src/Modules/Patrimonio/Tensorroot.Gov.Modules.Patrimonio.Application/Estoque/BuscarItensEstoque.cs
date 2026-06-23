using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Application.Common;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Estoque;

/// <summary>Item da lista de itens de almoxarifado (navegabilidade — Onda 0): projecao enxuta.</summary>
/// <param name="Id">Identificador do item.</param>
/// <param name="Codigo">Codigo no catalogo.</param>
/// <param name="Descricao">Descricao do item.</param>
/// <param name="UnidadeMedida">Unidade de medida.</param>
/// <param name="Saldo">Saldo atual.</param>
/// <param name="CustoMedio">Custo medio ponderado.</param>
/// <param name="ClassificacaoAbc">Classe na Curva ABC.</param>
/// <param name="Situacao">Situacao (ativo/inativo).</param>
public sealed record ItemEstoqueItemLista(
    Guid Id,
    string Codigo,
    string Descricao,
    string UnidadeMedida,
    decimal Saldo,
    decimal CustoMedio,
    string ClassificacaoAbc,
    string Situacao);

/// <summary>
/// Lista/busca paginada de itens de almoxarifado (navegabilidade — Onda 0). Tenant-scoped via Global
/// Query Filter; read-only. Filtra por termo livre (codigo/descricao) e, opcionalmente, situacao e classe ABC.
/// </summary>
/// <param name="Termo">Termo livre (codigo ou descricao); nulo lista tudo.</param>
/// <param name="Situacao">Filtro opcional por situacao.</param>
/// <param name="ClassificacaoAbc">Filtro opcional por classe na Curva ABC.</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarItensEstoqueQuery(
    string? Termo,
    SituacaoItemEstoque? Situacao,
    CurvaABC? ClassificacaoAbc,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<ItemEstoqueItemLista>>;

/// <summary>Handler da busca paginada de itens de almoxarifado.</summary>
public sealed class BuscarItensEstoqueHandler(IItemEstoqueRepository itens)
    : IQueryHandler<BuscarItensEstoqueQuery, ResultadoPaginado<ItemEstoqueItemLista>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<ItemEstoqueItemLista>> Handle(
        BuscarItensEstoqueQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var (resultado, total) = await itens
            .BuscarAsync(request.Termo, request.Situacao, request.ClassificacaoAbc, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = resultado
            .Select(item => new ItemEstoqueItemLista(
                item.Id.Value,
                item.Codigo,
                item.Descricao,
                item.UnidadeMedida,
                item.Saldo.Quantidade,
                item.CustoMedio.Valor,
                item.ClassificacaoAbc.ToString(),
                item.Situacao.ToString()))
            .ToList();

        return new ResultadoPaginado<ItemEstoqueItemLista>(projetados, total, pagina, tamanho);
    }
}
