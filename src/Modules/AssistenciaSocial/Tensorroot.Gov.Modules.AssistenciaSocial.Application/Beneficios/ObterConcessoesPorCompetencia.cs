using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Beneficios;

/// <summary>Lista as concessoes de uma competencia (apenas concedidas, tenant-scoped).</summary>
/// <param name="Competencia">Competencia (ano/mes) de referencia.</param>
public sealed record ObterConcessoesPorCompetenciaQuery(Competencia Competencia)
    : IQuery<IReadOnlyList<BeneficioResumo>>;

/// <summary>Handler da consulta de concessoes por competencia.</summary>
public sealed class ObterConcessoesPorCompetenciaHandler(IBeneficioRepository beneficios)
    : IQueryHandler<ObterConcessoesPorCompetenciaQuery, IReadOnlyList<BeneficioResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<BeneficioResumo>> Handle(
        ObterConcessoesPorCompetenciaQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontrados = await beneficios
            .ListarConcedidosPorCompetenciaAsync(request.Competencia, cancellationToken)
            .ConfigureAwait(false);

        return encontrados.Select(ObterBeneficiosDaFamiliaHandler.Projetar).ToList();
    }
}
