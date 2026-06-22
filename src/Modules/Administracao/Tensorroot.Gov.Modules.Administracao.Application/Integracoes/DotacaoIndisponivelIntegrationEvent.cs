using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Administracao.Application.Integracoes;

/// <summary>
/// Contrato (DTO) do evento de integracao publicado pelo modulo Financas quando nao ha dotacao/credito
/// orcamentario disponivel para um contrato (LRF). Administracao o consome para manter o contrato sem
/// eficacia e impedir o inicio da execucao (I-8). Definido localmente como Anti-Corruption Layer enquanto
/// o modulo Financas (e seu <c>Contracts</c>) ainda nao foi gerado; ao ser gerado, esta definicao passa
/// a residir em <c>Tensorroot.Gov.Modules.Financas.Contracts</c>. Idempotente por <c>EventId</c> no consumo.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="ContratoId">Identificador do contrato sem cobertura orcamentaria.</param>
/// <param name="Motivo">Motivo da indisponibilidade de dotacao.</param>
public sealed record DotacaoIndisponivelIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ContratoId,
    string Motivo) : IntegrationEvent(EventId, OccurredOnUtc);
