using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Transparencia.Contracts;

/// <summary>
/// Evento de integração público: uma remessa foi transmitida ao TCE-RS (SIAPC/PAD), permitindo
/// que outros Bounded Contexts (ex.: Finanças/painéis de gestão fiscal) registrem a prestação de
/// contas do período. Publicado transacionalmente via Outbox. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="RemessaTceId">Identificador da remessa enviada.</param>
/// <param name="Periodo">Período (competência/exercício) da remessa.</param>
/// <param name="DataEnvio">Data de transmissão ao SIAPC/PAD.</param>
public sealed record RemessaEnviadaTceIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid RemessaTceId,
    string Periodo,
    DateOnly DataEnvio) : IntegrationEvent(EventId, OccurredOnUtc);
