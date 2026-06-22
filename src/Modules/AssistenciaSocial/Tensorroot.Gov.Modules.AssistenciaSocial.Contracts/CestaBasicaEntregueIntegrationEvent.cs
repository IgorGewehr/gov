using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Contracts;

/// <summary>
/// Evento de integracao publico: uma cesta basica (beneficio eventual) foi entregue. O payload
/// expoe apenas identificadores, quantidade e data — sem dado sensivel identificavel (NIS/CPF/
/// saude) no barramento (Beneficio I-12; README secao 7). Idempotente por <c>EventId</c> no
/// consumidor (reentrega idempotente — B-10).
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (municipio) dono do registro.</param>
/// <param name="BeneficioId">Identificador do beneficio.</param>
/// <param name="FamiliaId">Familia beneficiaria.</param>
/// <param name="Quantidade">Quantidade de cestas entregues.</param>
/// <param name="DataEntrega">Data da entrega.</param>
public sealed record CestaBasicaEntregueIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid BeneficioId,
    Guid FamiliaId,
    int Quantidade,
    DateOnly DataEntrega) : IntegrationEvent(EventId, OccurredOnUtc);
