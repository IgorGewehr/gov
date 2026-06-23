using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Contracts;

/// <summary>
/// A-2: evento de integracao publico — o Registro Mensal de Atendimentos (RMA) de uma unidade foi
/// FECHADO numa competencia, selando-a para envio ao MDS (RMA/SAGI, mensal ate 30 dias apos o mes —
/// Res. CIT 4/2011 e 20/2013). Expoe apenas <b>volumes agregados</b> — nunca dado sigiloso identificavel
/// do prontuario (LGPD art. 11). Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (municipio) dono do registro.</param>
/// <param name="RmaId">Identificador do RMA fechado.</param>
/// <param name="UnidadeId">Unidade (CRAS/CREAS/Centro POP) consolidada.</param>
/// <param name="Competencia">Competencia (ano/mes) de referencia.</param>
/// <param name="TotalAtendimentos">Total de atendimentos consolidados na competencia.</param>
public sealed record RmaFechadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid RmaId,
    Guid UnidadeId,
    string Competencia,
    int TotalAtendimentos) : IntegrationEvent(EventId, OccurredOnUtc);
