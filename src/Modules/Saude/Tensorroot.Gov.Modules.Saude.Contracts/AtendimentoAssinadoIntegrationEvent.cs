using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Saude.Contracts;

/// <summary>
/// Evento de integracao publico: um atendimento clinico foi assinado em ICP-Brasil (NGS2),
/// tornando-se imutavel. Permite que outros Bounded Contexts (ex.: Transparencia, de forma
/// agregada/anonimizada) reajam ao fato. Carrega apenas identificadores — nunca conteudo
/// clinico bruto (LGPD art. 11). Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="AtendimentoId">Identificador do atendimento assinado.</param>
/// <param name="PacienteId">Identificador do paciente atendido (referencia por Id).</param>
public sealed record AtendimentoAssinadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid AtendimentoId,
    Guid PacienteId) : IntegrationEvent(EventId, OccurredOnUtc);
