using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Transparencia.Contracts;

/// <summary>
/// Evento de integração público: uma remessa foi rejeitada no e-Validador (RDI com erro) e o envio
/// está bloqueado, permitindo que outros Bounded Contexts alertem a gestão sobre a pendência de
/// prestação de contas. Publicado transacionalmente via Outbox. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="RemessaTceId">Identificador da remessa rejeitada.</param>
/// <param name="Periodo">Período (competência/exercício) da remessa.</param>
/// <param name="QuantidadeErros">Quantidade de erros apurados no RDI.</param>
public sealed record RemessaRejeitadaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid RemessaTceId,
    string Periodo,
    int QuantidadeErros) : IntegrationEvent(EventId, OccurredOnUtc);
