using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Application.Common;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Bens;

/// <summary>Item da lista de bens (navegabilidade — Onda 0): projecao enxuta para a tabela.</summary>
/// <param name="Id">Identificador do bem.</param>
/// <param name="NumeroTombamento">Numero de tombo, se tombado.</param>
/// <param name="Descricao">Descricao do bem.</param>
/// <param name="Tipo">Tipo (movel/imovel).</param>
/// <param name="ValorContabil">Valor contabil atual.</param>
/// <param name="DataIncorporacao">Data de incorporacao.</param>
/// <param name="Situacao">Situacao no ciclo patrimonial.</param>
public sealed record BemPatrimonialItemLista(
    Guid Id,
    string? NumeroTombamento,
    string Descricao,
    string Tipo,
    decimal ValorContabil,
    DateOnly DataIncorporacao,
    string Situacao);

/// <summary>
/// Lista/busca paginada de bens patrimoniais (navegabilidade — Onda 0). Tenant-scoped via Global
/// Query Filter; read-only. Filtra por termo livre (descricao/tombamento) e, opcionalmente, tipo e situacao.
/// </summary>
/// <param name="Termo">Termo livre (descricao ou numero de tombamento); nulo lista tudo.</param>
/// <param name="Tipo">Filtro opcional por tipo.</param>
/// <param name="Situacao">Filtro opcional por situacao.</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarBensQuery(
    string? Termo,
    TipoBem? Tipo,
    SituacaoBemPatrimonial? Situacao,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<BemPatrimonialItemLista>>;

/// <summary>Handler da busca paginada de bens.</summary>
public sealed class BuscarBensHandler(IBemPatrimonialRepository bens)
    : IQueryHandler<BuscarBensQuery, ResultadoPaginado<BemPatrimonialItemLista>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<BemPatrimonialItemLista>> Handle(
        BuscarBensQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var (itens, total) = await bens
            .BuscarAsync(request.Termo, request.Tipo, request.Situacao, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens
            .Select(bem => new BemPatrimonialItemLista(
                bem.Id.Value,
                bem.NumeroTombamento?.Valor,
                bem.Descricao,
                bem.Tipo.ToString(),
                bem.ValorContabil.Valor,
                bem.DataIncorporacao,
                bem.Situacao.ToString()))
            .ToList();

        return new ResultadoPaginado<BemPatrimonialItemLista>(projetados, total, pagina, tamanho);
    }
}
