using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Integracoes;

/// <summary>
/// Contrato (DTO) do evento de integracao publicado por outros modulos para solicitar a tramitacao
/// de um processo ao setor indicado (ex.: transicoes de fluxo BPMN). Definido localmente como
/// Anti-Corruption Layer enquanto os modulos de origem (e seus <c>Contracts</c>) ainda nao foram
/// gerados; ao serem gerados, esta definicao passara a residir no <c>Contracts</c> do modulo de
/// origem. Idempotente por <c>EventId</c> no consumo.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="Nup">Numero Unico de Protocolo do processo a tramitar.</param>
/// <param name="SetorDestinoId">Setor de destino solicitado.</param>
public sealed record TramitarProcessoRequested(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    string Nup,
    Guid SetorDestinoId) : IntegrationEvent(EventId, OccurredOnUtc);
