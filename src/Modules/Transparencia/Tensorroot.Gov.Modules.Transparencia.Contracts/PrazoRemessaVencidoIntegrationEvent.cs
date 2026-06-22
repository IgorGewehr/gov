using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Transparencia.Contracts;

/// <summary>
/// Evento de integração público: o prazo legal de uma remessa venceu sem envio, sinalizando risco
/// de bloqueio de transferências voluntárias (LRF art. 23 §3º). Permite que outros Bounded Contexts
/// alertem a gestão. Publicado transacionalmente via Outbox. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="RemessaTceId">Identificador da remessa com prazo vencido.</param>
/// <param name="Periodo">Período (competência/exercício) da remessa.</param>
/// <param name="DataLimite">Data-limite legal/parametrizada do período.</param>
public sealed record PrazoRemessaVencidoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid RemessaTceId,
    string Periodo,
    DateOnly DataLimite) : IntegrationEvent(EventId, OccurredOnUtc);
