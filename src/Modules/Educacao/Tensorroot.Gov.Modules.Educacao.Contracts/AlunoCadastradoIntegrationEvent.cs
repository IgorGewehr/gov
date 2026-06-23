using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Educacao.Contracts;

/// <summary>
/// Evento de integracao publico: um aluno foi cadastrado na rede de ensino (Onda 1). Consumivel por
/// outros Bounded Contexts (Transparencia, indicadores) via Outbox. Idempotente por <c>EventId</c> no
/// consumidor. NAO carrega PII de menor (LGPD art. 14) — apenas identificadores e tenant.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente municipal/rede) dono do registro.</param>
/// <param name="AlunoId">Identificador do aluno cadastrado.</param>
public sealed record AlunoCadastradoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid AlunoId) : IntegrationEvent(EventId, OccurredOnUtc);
