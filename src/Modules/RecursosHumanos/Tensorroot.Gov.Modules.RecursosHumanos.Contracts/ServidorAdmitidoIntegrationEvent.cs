using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Contracts;

/// <summary>
/// Evento de integracao publico: um servidor foi admitido (provimento em cargo), permitindo
/// que Cadastro/Patrimonio reajam (ex.: alocacao). Consumivel por outros Bounded Contexts.
/// Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="ServidorId">Identificador do servidor admitido.</param>
/// <param name="Matricula">Matricula unica do vinculo no tenant.</param>
/// <param name="CargoId">Identificador do cargo provido.</param>
public sealed record ServidorAdmitidoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ServidorId,
    string Matricula,
    Guid CargoId) : IntegrationEvent(EventId, OccurredOnUtc);
