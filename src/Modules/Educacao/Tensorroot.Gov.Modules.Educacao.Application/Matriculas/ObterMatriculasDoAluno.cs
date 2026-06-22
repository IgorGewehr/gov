using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Matriculas;

/// <summary>Lista as matriculas de um aluno (sempre tenant-scoped via Global Query Filter).</summary>
/// <param name="AlunoId">Aluno.</param>
public sealed record ObterMatriculasDoAlunoQuery(Guid AlunoId)
    : IQuery<IReadOnlyList<MatriculaResumo>>;

/// <summary>Handler da consulta de matriculas do aluno.</summary>
public sealed class ObterMatriculasDoAlunoHandler(IMatriculaRepository matriculas)
    : IQueryHandler<ObterMatriculasDoAlunoQuery, IReadOnlyList<MatriculaResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MatriculaResumo>> Handle(
        ObterMatriculasDoAlunoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontradas = await matriculas
            .ListarPorAlunoAsync(new AlunoId(request.AlunoId), cancellationToken)
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
