using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Contracts;

/// <summary>
/// Evento de integracao publico: o vinculo de um servidor foi encerrado, permitindo que
/// Acesso/Patrimonio revoguem acessos e recolham bens. Consumivel por outros Bounded
/// Contexts. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="ServidorId">Identificador do servidor desligado.</param>
/// <param name="DataDesligamento">Data de encerramento do vinculo.</param>
public sealed record ServidorDesligadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ServidorId,
    DateOnly DataDesligamento) : IntegrationEvent(EventId, OccurredOnUtc);
