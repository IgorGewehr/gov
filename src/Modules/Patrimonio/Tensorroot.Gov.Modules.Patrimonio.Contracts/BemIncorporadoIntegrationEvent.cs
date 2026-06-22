using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Patrimonio.Contracts;

/// <summary>
/// Evento de integração público: um bem patrimonial foi incorporado ao acervo (variação
/// patrimonial aumentativa), permitindo que Finanças reconheça o ingresso contábil (MCASP).
/// Consumível por outros Bounded Contexts. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="BemPatrimonialId">Identificador do bem incorporado.</param>
/// <param name="ValorInicial">Valor de incorporação (custo de ingresso).</param>
/// <param name="Origem">Origem do ingresso (aquisição, doação, produção própria).</param>
public sealed record BemIncorporadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid BemPatrimonialId,
    decimal ValorInicial,
    string Origem) : IntegrationEvent(EventId, OccurredOnUtc);
