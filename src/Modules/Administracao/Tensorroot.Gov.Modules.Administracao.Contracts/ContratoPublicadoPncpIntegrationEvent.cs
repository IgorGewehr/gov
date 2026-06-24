using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Administracao.Contracts;

/// <summary>
/// Evento de integracao publico: um contrato foi divulgado no PNCP — condicao de eficacia
/// (Lei 14.133/2021, art. 94; gestao do PNCP pelo Decreto 10.764/2021 — o art. 174 institui o PNCP e o
/// Dec. 11.462/2023 trata de SRP, nao da eficacia do contrato). Consumido por Transparencia/PNCP
/// (publicidade). Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="ContratoId">Identificador do contrato publicado.</param>
/// <param name="NumeroContratoPncp">Identificador do contrato no PNCP.</param>
public sealed record ContratoPublicadoPncpIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ContratoId,
    string NumeroContratoPncp) : IntegrationEvent(EventId, OccurredOnUtc);
