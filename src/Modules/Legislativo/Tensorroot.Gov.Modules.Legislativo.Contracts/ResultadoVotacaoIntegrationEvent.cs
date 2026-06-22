using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Legislativo.Contracts;

/// <summary>
/// Evento de integracao publico: uma votacao foi encerrada e apurada, com o resultado
/// (Aprovado/Rejeitado) sobre a proposicao. Alimenta a tramitacao da proposicao
/// (Aprovar/Rejeitar) e o portal de transparencia (LAI/APIs abertas), respeitando o sigilo de
/// votacoes secretas (apenas o placar agregado). Consumivel por outros Bounded Contexts.
/// Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (Camara) dono do registro.</param>
/// <param name="VotacaoId">Identificador da votacao encerrada.</param>
/// <param name="ProposicaoId">Materia (proposicao) votada.</param>
/// <param name="Resultado">Resultado apurado (Aprovado / Rejeitado).</param>
public sealed record ResultadoVotacaoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid VotacaoId,
    Guid ProposicaoId,
    string Resultado) : IntegrationEvent(EventId, OccurredOnUtc);
