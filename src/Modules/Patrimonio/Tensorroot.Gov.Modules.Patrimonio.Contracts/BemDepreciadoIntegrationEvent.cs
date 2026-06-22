using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Patrimonio.Contracts;

/// <summary>
/// Evento de integração público: a depreciação de uma competência foi reconhecida sobre o bem,
/// permitindo que Finanças contabilize a variação patrimonial diminutiva (MCASP). Consumível por
/// outros Bounded Contexts. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="BemPatrimonialId">Identificador do bem depreciado.</param>
/// <param name="ValorDepreciado">Valor depreciado na competência.</param>
/// <param name="Competencia">Mês/ano de referência do reconhecimento.</param>
public sealed record BemDepreciadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid BemPatrimonialId,
    decimal ValorDepreciado,
    DateOnly Competencia) : IntegrationEvent(EventId, OccurredOnUtc);
