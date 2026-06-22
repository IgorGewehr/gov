using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Integracoes;

/// <summary>
/// Contrato (DTO) do evento de integracao publicado por outros modulos para solicitar o arquivamento
/// do processo por eles originado. Definido localmente como Anti-Corruption Layer enquanto os modulos
/// de origem (e seus <c>Contracts</c>) ainda nao foram gerados; ao serem gerados, esta definicao
/// passara a residir no <c>Contracts</c> do modulo de origem. Idempotente por <c>EventId</c> no consumo.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="Nup">Numero Unico de Protocolo do processo a arquivar.</param>
/// <param name="Motivo">Motivo do arquivamento (opcional).</param>
public sealed record ArquivarProcessoRequested(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    string Nup,
    string? Motivo) : IntegrationEvent(EventId, OccurredOnUtc);
