using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Patrimonio.Contracts;

/// <summary>
/// Evento de integração público: um bem foi reavaliado a valor justo (ou ajustado por impairment),
/// permitindo que Finanças reconheça o ajuste de avaliação patrimonial (MCASP). Consumível por
/// outros Bounded Contexts. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="BemPatrimonialId">Identificador do bem reavaliado.</param>
/// <param name="NovoValor">Novo valor contábil resultante da reavaliação.</param>
public sealed record BemReavaliadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid BemPatrimonialId,
    decimal NovoValor) : IntegrationEvent(EventId, OccurredOnUtc);
