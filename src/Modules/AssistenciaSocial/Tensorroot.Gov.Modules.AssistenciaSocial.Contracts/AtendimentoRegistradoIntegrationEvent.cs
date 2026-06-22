using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Contracts;

/// <summary>
/// Evento de integracao publico: um atendimento socioassistencial (PAIF/PAEFI/SCFV) foi
/// registrado num prontuario. Expoe apenas <b>identificadores e metadados</b> (servico, data) —
/// nunca o conteudo sigiloso do acompanhamento (I-9). Alimenta o Registro Mensal de Atendimentos
/// (RMA) e consumidores autorizados. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (municipio) dono do registro.</param>
/// <param name="ProntuarioId">Identificador do prontuario.</param>
/// <param name="UnidadeId">Unidade de atendimento (CRAS/CREAS) responsavel.</param>
/// <param name="Servico">Servico socioassistencial (PAIF/PAEFI/SCFV).</param>
/// <param name="DataAtendimento">Data do atendimento.</param>
public sealed record AtendimentoRegistradoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ProntuarioId,
    Guid UnidadeId,
    string Servico,
    DateOnly DataAtendimento) : IntegrationEvent(EventId, OccurredOnUtc);
