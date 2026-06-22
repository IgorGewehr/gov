using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

namespace Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;

/// <summary>Projecao resumida do diario de uma matricula para leitura.</summary>
/// <param name="Id">Identificador do diario.</param>
/// <param name="MatriculaId">Matricula vinculada.</param>
/// <param name="Situacao">Situacao atual do diario.</param>
/// <param name="PercentualFrequencia">Percentual de frequencia calculado.</param>
/// <param name="Resultado">Resultado apurado, quando houver.</param>
/// <param name="DiasLetivosRegistrados">Quantidade de dias letivos registrados.</param>
public sealed record DiarioClasseResumo(
    Guid Id,
    Guid MatriculaId,
    string Situacao,
    decimal PercentualFrequencia,
    string? Resultado,
    int DiasLetivosRegistrados);

/// <summary>Obtem o diario vinculado a uma matricula (tenant-scoped — I-7).</summary>
/// <param name="MatriculaId">Matricula a consultar.</param>
public sealed record ObterDiarioDaMatriculaQuery(Guid MatriculaId) : IQuery<DiarioClasseResumo?>;

/// <summary>Handler da consulta do diario da matricula.</summary>
public sealed class ObterDiarioDaMatriculaHandler(IDiarioClasseRepository diarios)
    : IQueryHandler<ObterDiarioDaMatriculaQuery, DiarioClasseResumo?>
{
    /// <inheritdoc />
    public async Task<DiarioClasseResumo?> Handle(ObterDiarioDaMatriculaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diario = await diarios.ObterPorMatriculaAsync(new MatriculaId(request.MatriculaId), cancellationToken).ConfigureAwait(false);
        if (diario is null)
        {
            return null;
        }

        return new DiarioClasseResumo(
            diario.Id.Value,
            diario.MatriculaId.Value,
            diario.Situacao.ToString(),
            diario.PercentualFrequencia,
            diario.Resultado?.ToString(),
            diario.DiasLetivosRegistrados);
    }
}
