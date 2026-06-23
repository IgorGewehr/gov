using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Application.Common;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Inventarios;

/// <summary>Item da lista de inventários: projeção enxuta para a tabela.</summary>
/// <param name="Id">Identificador do inventário.</param>
/// <param name="Exercicio">Exercício (ano-base).</param>
/// <param name="Tipo">Tipo do inventário.</param>
/// <param name="Setor">Setor escopo (nulo = geral).</param>
/// <param name="Situacao">Situação no levantamento.</param>
/// <param name="DataAbertura">Data de abertura.</param>
/// <param name="DataEncerramento">Data de encerramento (se encerrado).</param>
/// <param name="TotalItens">Quantidade de itens do snapshot.</param>
/// <param name="TotalDivergencias">Quantidade de divergências apuradas.</param>
public sealed record InventarioItemLista(
    Guid Id,
    int Exercicio,
    string Tipo,
    string? Setor,
    string Situacao,
    DateOnly DataAbertura,
    DateOnly? DataEncerramento,
    int TotalItens,
    int TotalDivergencias);

/// <summary>
/// Lista/busca paginada de inventários por exercício/setor/situação. Tenant-scoped via Global Query
/// Filter; read-only. Ordena por exercício decrescente e data de abertura.
/// </summary>
/// <param name="Exercicio">Filtro opcional por exercício.</param>
/// <param name="Setor">Filtro opcional por setor (case-insensível).</param>
/// <param name="Situacao">Filtro opcional por situação.</param>
/// <param name="Pagina">Página (base 1).</param>
/// <param name="Tamanho">Tamanho da página.</param>
public sealed record BuscarInventariosQuery(
    int? Exercicio,
    string? Setor,
    SituacaoInventario? Situacao,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<InventarioItemLista>>;

/// <summary>Handler da busca paginada de inventários.</summary>
public sealed class BuscarInventariosHandler(IInventarioRepository inventarios)
    : IQueryHandler<BuscarInventariosQuery, ResultadoPaginado<InventarioItemLista>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<InventarioItemLista>> Handle(
        BuscarInventariosQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var (itens, total) = await inventarios
            .BuscarAsync(request.Exercicio, request.Setor, request.Situacao, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens
            .Select(inventario => new InventarioItemLista(
                inventario.Id.Value,
                inventario.Exercicio,
                inventario.Tipo.ToString(),
                inventario.Setor,
                inventario.Situacao.ToString(),
                inventario.DataAbertura,
                inventario.DataEncerramento,
                inventario.Itens.Count,
                inventario.Divergencias.Count))
            .ToList();

        return new ResultadoPaginado<InventarioItemLista>(projetados, total, pagina, tamanho);
    }
}
