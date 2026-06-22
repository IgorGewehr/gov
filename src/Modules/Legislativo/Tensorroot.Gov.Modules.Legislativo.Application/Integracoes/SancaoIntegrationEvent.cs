using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Integracoes;

/// <summary>
/// Contrato (DTO) do evento de integracao publicado pelo modulo do Executivo quando o prefeito
/// sanciona (expressa ou tacitamente) o autografo de uma proposicao. O Legislativo o consome para
/// atualizar a trilha de <c>Tramitacao</c> da proposicao (I-13). Definido localmente como
/// Anti-Corruption Layer enquanto o modulo do Executivo (e seu <c>Contracts</c>) ainda nao foi
/// gerado; ao ser gerado, esta definicao passara a residir no <c>Contracts</c> da origem (Executivo).
/// Idempotente por <c>EventId</c> no consumo.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (Camara) dono do registro.</param>
/// <param name="ProposicaoId">Identificador da proposicao sancionada.</param>
public sealed record SancaoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ProposicaoId) : IntegrationEvent(EventId, OccurredOnUtc);
