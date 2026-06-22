using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Saude.Contracts;

/// <summary>
/// Evento de integracao publico: o RES de um atendimento foi compartilhado e aceito na RNDS
/// (Bundle FHIR R4 via mTLS + ICP-Brasil). A publicacao para Transparencia usa dados
/// agregados/anonimizados; este contrato carrega apenas identificadores e o protocolo da RNDS,
/// nunca conteudo clinico bruto (LGPD art. 11). Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="AtendimentoId">Identificador do atendimento compartilhado.</param>
/// <param name="ProtocoloRnds">Protocolo de aceite retornado pela RNDS.</param>
public sealed record RESCompartilhadoNaRNDSIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid AtendimentoId,
    string ProtocoloRnds) : IntegrationEvent(EventId, OccurredOnUtc);
