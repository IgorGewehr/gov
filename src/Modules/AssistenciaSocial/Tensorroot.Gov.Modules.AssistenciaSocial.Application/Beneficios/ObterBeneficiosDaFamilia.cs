using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Beneficios;

/// <summary>Resumo de um beneficio para leitura (projecao tenant-scoped).</summary>
/// <param name="Id">Identificador do beneficio.</param>
/// <param name="FamiliaId">Familia beneficiaria.</param>
/// <param name="Tipo">Tipo do beneficio.</param>
/// <param name="Competencia">Competencia de referencia (mm/aaaa).</param>
/// <param name="Valor">Valor concedido, quando houver.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="MotivoIndeferimento">Motivo do indeferimento, quando indeferido.</param>
/// <param name="DataDecisao">Data da decisao, quando decidido.</param>
public sealed record BeneficioResumo(
    Guid Id,
    Guid FamiliaId,
    string Tipo,
    string Competencia,
    decimal? Valor,
    string Situacao,
    string? MotivoIndeferimento,
    DateOnly? DataDecisao);

/// <summary>Lista os beneficios de uma familia (tenant-scoped).</summary>
/// <param name="FamiliaId">Familia.</param>
public sealed record ObterBeneficiosDaFamiliaQuery(Guid FamiliaId)
    : IQuery<IReadOnlyList<BeneficioResumo>>;

/// <summary>Handler da consulta de beneficios da familia.</summary>
public sealed class ObterBeneficiosDaFamiliaHandler(IBeneficioRepository beneficios)
    : IQueryHandler<ObterBeneficiosDaFamiliaQuery, IReadOnlyList<BeneficioResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<BeneficioResumo>> Handle(
        ObterBeneficiosDaFamiliaQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontrados = await beneficios
            .ListarPorFamiliaAsync(request.FamiliaId, cancellationToken)
            .ConfigureAwait(false);

        return encontrados.Select(Projetar).ToList();
    }

    internal static BeneficioResumo Projetar(Beneficio beneficio) => new(
        beneficio.Id.Value,
        beneficio.FamiliaId,
        beneficio.Tipo.ToString(),
        beneficio.Competencia.ToString(),
        beneficio.Valor?.Valor,
        beneficio.Situacao.ToString(),
        beneficio.MotivoIndeferimento,
        beneficio.DataDecisao);
}
