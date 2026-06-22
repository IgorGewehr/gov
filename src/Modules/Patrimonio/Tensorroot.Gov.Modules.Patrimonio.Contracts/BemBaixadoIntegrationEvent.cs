using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Patrimonio.Contracts;

/// <summary>
/// Evento de integração público: um bem foi baixado/alienado do acervo (variação patrimonial
/// diminutiva), permitindo que Finanças reconheça a baixa contábil (MCASP). Consumível por outros
/// Bounded Contexts. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="BemPatrimonialId">Identificador do bem baixado.</param>
/// <param name="MotivoBaixa">Motivo da baixa (ou "Alienacao").</param>
/// <param name="ValorContabil">Valor contábil no momento da baixa.</param>
public sealed record BemBaixadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid BemPatrimonialId,
    string MotivoBaixa,
    decimal ValorContabil) : IntegrationEvent(EventId, OccurredOnUtc);
