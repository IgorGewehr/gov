using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Patrimonio.Contracts;

/// <summary>
/// Evento de integração público: um item de almoxarifado atingiu o ponto de pedido após uma
/// saída, disparando a reposição/compra na Administração (Lei 14.133). Consumível por outros
/// Bounded Contexts. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="ItemId">Identificador do item de estoque que atingiu o ponto de pedido.</param>
/// <param name="SaldoAtual">Saldo resultante após a baixa.</param>
public sealed record PontoPedidoAtingidoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ItemId,
    decimal SaldoAtual) : IntegrationEvent(EventId, OccurredOnUtc);
