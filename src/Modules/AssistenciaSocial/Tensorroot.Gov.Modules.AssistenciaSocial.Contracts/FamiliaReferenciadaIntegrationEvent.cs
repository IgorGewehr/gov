using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Contracts;

/// <summary>
/// Evento de integracao publico: uma familia foi referenciada a uma Unidade de Atendimento (CRAS).
/// Disponivel a modulos autorizados (ex.: Transparencia — dados agregados/anonimizados). O NIS
/// trafega mascarado e nenhum dado sensivel identificavel ou do CadUnico federal e exposto no
/// barramento (README secao 7; I-9). Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (municipio) dono do registro.</param>
/// <param name="FamiliaId">Identificador da familia referenciada.</param>
/// <param name="NisMascarado">NIS do responsavel familiar, mascarado.</param>
/// <param name="UnidadeId">Identificador da Unidade de Atendimento (CRAS).</param>
/// <param name="Territorio">Territorio de cobertura ao qual a familia pertence.</param>
/// <param name="DataReferenciamento">Data do referenciamento ao CRAS.</param>
public sealed record FamiliaReferenciadaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid FamiliaId,
    string NisMascarado,
    Guid UnidadeId,
    string Territorio,
    DateOnly DataReferenciamento) : IntegrationEvent(EventId, OccurredOnUtc);
