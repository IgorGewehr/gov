using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Administracao.Contracts;

/// <summary>
/// Evento de integracao publico: uma dispensa eletronica em razao do valor foi homologada (Lei
/// 14.133/2021, art. 75; IN SEGES/ME 67/2021), habilitando a formalizacao do contrato/empenho
/// decorrente da contratacao direta. Consumivel por outros Bounded Contexts (ex.: Financas para o
/// empenho). Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="DispensaId">Identificador da dispensa homologada.</param>
/// <param name="FornecedorVencedorId">Identificador do fornecedor vencedor.</param>
/// <param name="ValorAdjudicado">Valor adjudicado (total agregado da proposta vencedora).</param>
public sealed record DispensaHomologadaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid DispensaId,
    Guid FornecedorVencedorId,
    decimal ValorAdjudicado) : IntegrationEvent(EventId, OccurredOnUtc);
