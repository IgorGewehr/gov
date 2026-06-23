using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Matriculas;

/// <summary>Lista a Matricula Inicial de uma turma na data de referencia do Censo (tenant-scoped).</summary>
/// <param name="TurmaId">Turma.</param>
/// <param name="DataReferencia">Data de referencia do Censo (Matricula Inicial).</param>
public sealed record ListarMatriculasInicialDaTurmaQuery(Guid TurmaId, DateOnly DataReferencia)
    : IQuery<IReadOnlyList<MatriculaResumo>>;

/// <summary>Handler da consulta da Matricula Inicial da turma.</summary>
public sealed class ListarMatriculasInicialDaTurmaHandler(IMatriculaRepository matriculas)
    : IQueryHandler<ListarMatriculasInicialDaTurmaQuery, IReadOnlyList<MatriculaResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MatriculaResumo>> Handle(
        ListarMatriculasInicialDaTurmaQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontradas = await matriculas
            .ListarPorTurmaEDataReferenciaAsync(new TurmaId(request.TurmaId), request.DataReferencia, cancellationToken)
            .ConfigureAwait(false);

        return encontradas
            .Select(matricula => new MatriculaResumo(
                matricula.Id.Value,
                matricula.AlunoId.Value,
                matricula.TurmaId.Value,
                matricula.EscolaId.Value,
                matricula.Situacao.ToString(),
                matricula.DataReferencia))
            .ToList();
    }
}
