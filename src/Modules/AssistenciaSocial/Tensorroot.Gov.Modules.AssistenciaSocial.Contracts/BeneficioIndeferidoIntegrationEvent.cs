using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Contracts;

/// <summary>
/// Evento de integracao publico: a concessao de um beneficio foi indeferida, com motivo
/// fundamentado. O payload expoe apenas identificadores e o motivo — sem dado sensivel
/// identificavel (NIS/CPF/saude) no barramento (Beneficio I-12; README secao 7). Idempotente
/// por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (municipio) dono do registro.</param>
/// <param name="BeneficioId">Identificador do beneficio indeferido.</param>
/// <param name="FamiliaId">Familia requerente.</param>
/// <param name="Tipo">Tipo do beneficio avaliado (Bpc/Pbf/Eventual).</param>
/// <param name="MotivoIndeferimento">Motivo da negativa.</param>
public sealed record BeneficioIndeferidoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid BeneficioId,
    Guid FamiliaId,
    string Tipo,
    string MotivoIndeferimento) : IntegrationEvent(EventId, OccurredOnUtc);
