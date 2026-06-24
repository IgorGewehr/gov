using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Administracao.Contracts;

/// <summary>
/// Evento de integracao publico: o prazo legal de DIVULGACAO de um contrato no PNCP (Lei 14.133/2021,
/// art. 94) esta A VENCER (dentro da janela de antecedencia do tenant) sem que o contrato tenha sido
/// divulgado. Consumido pelo Portal do Gestor (alerta preventivo de eficacia do contrato — art. 94).
/// Publicado transacionalmente via Outbox. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="ContratoId">Identificador do contrato com prazo a vencer.</param>
/// <param name="DataLimite">Data-limite legal da divulgacao no PNCP.</param>
/// <param name="DiasUteisRestantes">Dias uteis restantes ate o vencimento.</param>
public sealed record PrazoPncpAVencerIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ContratoId,
    DateOnly DataLimite,
    int DiasUteisRestantes) : IntegrationEvent(EventId, OccurredOnUtc);

/// <summary>
/// Evento de integracao publico: o prazo legal de DIVULGACAO de um contrato no PNCP (Lei 14.133/2021,
/// art. 94) VENCEU sem divulgacao — o contrato permanece ineficaz e NAO empenha (invariante de bloqueio).
/// Consumido pelo Portal do Gestor (alerta de risco). Publicado transacionalmente via Outbox.
/// Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="ContratoId">Identificador do contrato com prazo vencido.</param>
/// <param name="DataLimite">Data-limite legal (vencida) da divulgacao no PNCP.</param>
public sealed record PrazoPncpVencidoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ContratoId,
    DateOnly DataLimite) : IntegrationEvent(EventId, OccurredOnUtc);
