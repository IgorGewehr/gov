using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;

namespace Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;

using DiarioClasseAggregate = Domain.DiarioClasse.DiarioClasse;

/// <summary>Frequencia consolidada de um diario para leitura (I-1/I-2).</summary>
/// <param name="DiarioClasseId">Identificador do diario.</param>
/// <param name="PercentualFrequencia">Percentual de frequencia calculado.</param>
/// <param name="AulasComputadas">Quantidade de registros de frequencia computados.</param>
/// <param name="AtingiuMinimo">Indica se atingiu o minimo de 75% (LDB).</param>
public sealed record FrequenciaConsolidada(
    Guid DiarioClasseId,
    decimal PercentualFrequencia,
    int AulasComputadas,
    bool AtingiuMinimo);

/// <summary>Obtem a frequencia consolidada de um diario (tenant-scoped).</summary>
/// <param name="DiarioClasseId">Diario a consultar.</param>
public sealed record ObterFrequenciaDoDiarioQuery(Guid DiarioClasseId) : IQuery<FrequenciaConsolidada>;

/// <summary>Handler da consulta de frequencia consolidada do diario.</summary>
public sealed class ObterFrequenciaDoDiarioHandler(IDiarioClasseRepository diarios)
    : IQueryHandler<ObterFrequenciaDoDiarioQuery, FrequenciaConsolidada>
{
    /// <inheritdoc />
    public async Task<FrequenciaConsolidada> Handle(ObterFrequenciaDoDiarioQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diario = await diarios.ObterPorIdAsync(new DiarioClasseId(request.DiarioClasseId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Diario nao encontrado.");

        return Projetar(diario);
    }

    private static FrequenciaConsolidada Projetar(DiarioClasseAggregate diario)
        => new(
            diario.Id.Value,
            diario.PercentualFrequencia,
            diario.Frequencias.Count,
            diario.PercentualFrequencia >= DiarioClasseAggregate.FrequenciaMinimaAprovacao);
}
