using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Tributos.Contracts;

/// <summary>
/// Evento de integração público: uma receita (ISS / Dívida Ativa) foi arrecadada no
/// módulo Tributos. Consumível por outros Bounded Contexts (ex.: Finanças).
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="OrigemId">Identificador de origem (ex.: dívida ativa quitada).</param>
/// <param name="Valor">Valor arrecadado.</param>
/// <param name="Data">Data da arrecadação.</param>
public sealed record ReceitaArrecadadaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid OrigemId,
    decimal Valor,
    DateOnly Data) : IntegrationEvent(EventId, OccurredOnUtc);
