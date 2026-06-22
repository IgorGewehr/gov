using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Contracts;

/// <summary>
/// Evento de integracao publico: o acompanhamento familiar de um prontuario foi encerrado, com
/// motivo. Expoe apenas <b>identificadores e metadados</b> — nunca o conteudo sigiloso (I-9).
/// Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (municipio) dono do registro.</param>
/// <param name="ProntuarioId">Identificador do prontuario.</param>
/// <param name="MotivoEncerramento">Motivo do encerramento do acompanhamento.</param>
public sealed record AcompanhamentoEncerradoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ProntuarioId,
    string MotivoEncerramento) : IntegrationEvent(EventId, OccurredOnUtc);
