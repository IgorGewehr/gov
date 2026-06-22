using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Protocolo.Contracts;

/// <summary>
/// Evento de integracao publico: um processo administrativo foi tramitado para um setor de destino.
/// Permite que modulos orquestradores (ex.: motor BPMN) reflitam a transicao do fluxo.
/// Consumivel por outros Bounded Contexts. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="Nup">Numero Unico de Protocolo do processo tramitado.</param>
/// <param name="SetorDestinoId">Setor de destino da tramitacao.</param>
public sealed record ProcessoTramitadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    string Nup,
    Guid SetorDestinoId) : IntegrationEvent(EventId, OccurredOnUtc);
