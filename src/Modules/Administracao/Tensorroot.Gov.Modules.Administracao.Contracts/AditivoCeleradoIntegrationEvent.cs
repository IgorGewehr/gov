using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Administracao.Contracts;

/// <summary>
/// Evento de integracao publico: um termo aditivo foi celebrado (Lei 14.133/2021, art. 125). Dispara a
/// divulgacao no PNCP (condicao de eficacia do aditivo — art. 94, cujo caput abrange "os contratos e seus
/// aditamentos"; o art. 174 apenas institui o PNCP) e o reforco de empenho em Financas. Idempotente por
/// <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="ContratoId">Identificador do contrato.</param>
/// <param name="AditivoId">Identificador do aditivo celebrado.</param>
/// <param name="ValorAtual">Valor vigente do contrato apos o aditivo.</param>
/// <param name="NovaVigenciaFim">Data-fim de vigencia vigente apos o aditivo.</param>
public sealed record AditivoCeleradoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ContratoId,
    Guid AditivoId,
    decimal ValorAtual,
    DateOnly NovaVigenciaFim) : IntegrationEvent(EventId, OccurredOnUtc);
