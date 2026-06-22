using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Protocolo.Contracts;

/// <summary>
/// Evento de integracao publico: um processo administrativo foi autuado e o NUP foi gerado.
/// Devolve o NUP ao modulo originador (ex.: Licitacoes, RH, Licencas) que solicitou a autuacao.
/// Consumivel por outros Bounded Contexts. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="Nup">Numero Unico de Protocolo gerado.</param>
/// <param name="OrigemModulo">Modulo originador (quando autuado por outro modulo).</param>
/// <param name="OrigemId">Identificador da origem no modulo originador.</param>
public sealed record ProcessoAutuadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    string Nup,
    string? OrigemModulo,
    Guid? OrigemId) : IntegrationEvent(EventId, OccurredOnUtc);
