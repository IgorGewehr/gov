using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Legislativo.Contracts;

/// <summary>
/// Evento de integracao publico: o autografo de uma proposicao aprovada foi gerado e remetido ao
/// Executivo (assinado ICP-Brasil) para sancao/veto/promulgacao. Publicado via Outbox (consistencia
/// transacional com o estado). Consumivel por outros Bounded Contexts (modulo do Executivo).
/// Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (Camara) dono do registro.</param>
/// <param name="ProposicaoId">Identificador da proposicao cujo autografo foi enviado.</param>
/// <param name="NumeroAutografo">Numero do autografo gerado.</param>
public sealed record AutografoEnviadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ProposicaoId,
    string NumeroAutografo) : IntegrationEvent(EventId, OccurredOnUtc);
