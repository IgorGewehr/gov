using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Administracao.Application.Integracoes;

/// <summary>
/// Contrato (DTO) do evento de integracao publicado pelo modulo Financas quando um empenho e emitido
/// para um contrato (Lei 4.320; LRF), confirmando a cobertura orcamentaria. Administracao o consome para
/// marcar <c>DotacaoConfirmada</c> e, eventualmente, tornar o contrato Eficaz (I-8). Definido localmente
/// como Anti-Corruption Layer enquanto o modulo Financas (e seu <c>Contracts</c>) ainda nao foi gerado;
/// ao ser gerado, esta definicao passa a residir em <c>Tensorroot.Gov.Modules.Financas.Contracts</c>.
/// Idempotente por <c>EventId</c> no consumo (deduplicacao no Inbox).
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="ContratoId">Identificador do contrato cuja dotacao foi reservada.</param>
/// <param name="EmpenhoId">Identificador do empenho emitido.</param>
/// <param name="NumeroEmpenho">Numero do empenho.</param>
/// <param name="Valor">Valor empenhado.</param>
public sealed record EmpenhoEmitidoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ContratoId,
    Guid EmpenhoId,
    string NumeroEmpenho,
    decimal Valor) : IntegrationEvent(EventId, OccurredOnUtc);
