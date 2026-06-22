using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Transparencia.Contracts;

/// <summary>
/// Evento de integracao publico: a Matriz de Saldos Contabeis (MSC) foi transmitida ao SICONFI (STN),
/// permitindo que outros Bounded Contexts reajam ao envio da prestacao de contas fiscal. Publicado
/// transacionalmente via Outbox. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="DeclaracaoFiscalId">Identificador da declaracao fiscal transmitida.</param>
/// <param name="Competencia">Competencia (MM/AAAA) da MSC.</param>
/// <param name="Protocolo">Protocolo retornado pelo SICONFI.</param>
/// <param name="DataTransmissao">Data de transmissao ao SICONFI.</param>
public sealed record MscEnviadaSiconfiIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid DeclaracaoFiscalId,
    string Competencia,
    string Protocolo,
    DateOnly DataTransmissao) : IntegrationEvent(EventId, OccurredOnUtc);
