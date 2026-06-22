using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Administracao.Contracts;

/// <summary>
/// Evento de integracao publico: um contrato foi celebrado/assinado (Lei 14.133/2021). Consumido pelo
/// modulo Financas para emitir o empenho (Lei 4.320; LRF). Consumivel por outros Bounded Contexts.
/// Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="ContratoId">Identificador do contrato celebrado.</param>
/// <param name="FornecedorId">Identificador do fornecedor contratado.</param>
/// <param name="Valor">Valor global do contrato.</param>
/// <param name="LicitacaoId">Identificador da licitacao de origem (quando houver).</param>
public sealed record ContratoAssinadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ContratoId,
    Guid FornecedorId,
    decimal Valor,
    Guid? LicitacaoId) : IntegrationEvent(EventId, OccurredOnUtc);
