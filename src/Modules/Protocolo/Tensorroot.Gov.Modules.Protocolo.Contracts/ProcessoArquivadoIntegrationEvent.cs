using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Protocolo.Contracts;

/// <summary>
/// Evento de integracao publico: um processo administrativo foi arquivado (terminal). Sinaliza ao
/// modulo originador o encerramento; a guarda passa a reger-se pela Tabela de Temporalidade (TTD/CONARQ).
/// Consumivel por outros Bounded Contexts. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="Nup">Numero Unico de Protocolo do processo arquivado.</param>
public sealed record ProcessoArquivadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    string Nup) : IntegrationEvent(EventId, OccurredOnUtc);
