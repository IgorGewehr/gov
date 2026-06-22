using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Administracao.Contracts;

/// <summary>
/// Evento de integracao publico: uma licitacao foi homologada (art. 71 da Lei 14.133/2021),
/// habilitando a formalizacao do contrato decorrente. Consumivel por outros Bounded Contexts.
/// Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="LicitacaoId">Identificador da licitacao homologada.</param>
/// <param name="FornecedorVencedorId">Identificador do fornecedor vencedor.</param>
/// <param name="ValorAdjudicado">Valor adjudicado (valor da proposta vencedora).</param>
public sealed record LicitacaoHomologadaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid LicitacaoId,
    Guid FornecedorVencedorId,
    decimal ValorAdjudicado) : IntegrationEvent(EventId, OccurredOnUtc);
