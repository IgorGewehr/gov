using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Protocolo.Contracts;

/// <summary>
/// Evento de integracao publico: um documento foi assinado conforme a criticidade do ato
/// (Lei 14.063/2020 + Decreto 10.543/2020). Informa os modulos originadores (Licitacoes/RH/Licencas)
/// que o ato processual foi assinado. Publicado via Outbox; idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="DocumentoId">Identificador do documento assinado.</param>
/// <param name="ProcessoId">Processo ao qual o documento esta juntado.</param>
/// <param name="TipoAssinatura">Nivel da assinatura aplicada (Simples/Avancada/Qualificada).</param>
public sealed record DocumentoAssinadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid DocumentoId,
    Guid ProcessoId,
    string TipoAssinatura) : IntegrationEvent(EventId, OccurredOnUtc);
