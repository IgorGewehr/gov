using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Application.Common;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Turmas;

/// <summary>
/// Busca paginada de turmas por escola/ano/turno/etapa/situacao (picker do front, com vagas disponiveis).
/// Tenant-scoped via Global Query Filter; read-only.
/// </summary>
/// <param name="EscolaId">Filtro opcional por escola.</param>
/// <param name="AnoLetivo">Filtro opcional por ano letivo.</param>
/// <param name="Turno">Filtro opcional por turno.</param>
/// <param name="Etapa">Filtro opcional por etapa.</param>
/// <param name="Situacao">Filtro opcional por situacao.</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarTurmasQuery(
    Guid? EscolaId,
    int? AnoLetivo,
    Turno? Turno,
    Etapa? Etapa,
    SituacaoTurma? Situacao,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<TurmaItemLista>>;

/// <summary>Handler da busca paginada de turmas.</summary>
public sealed class BuscarTurmasHandler(ITurmaRepository turmas)
    : IQueryHandler<BuscarTurmasQuery, ResultadoPaginado<TurmaItemLista>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<TurmaItemLista>> Handle(
        BuscarTurmasQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var escolaId = request.EscolaId is { } id ? new EscolaId(id) : (EscolaId?)null;

        var (itens, total) = await turmas
            .BuscarAsync(escolaId, request.AnoLetivo, request.Turno, request.Etapa, request.Situacao, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens.Select(TurmaMapeamento.ParaItemLista).ToList();
        return new ResultadoPaginado<TurmaItemLista>(projetados, total, pagina, tamanho);
    }
}
