using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Contracts;

/// <summary>
/// Evento de integracao publico: o liquido da folha foi pago, permitindo que a Tesouraria reconheca
/// a liquidacao financeira. Consumivel por outros Bounded Contexts. Idempotente por <c>EventId</c>
/// no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="FolhaDePagamentoId">Identificador da folha paga.</param>
/// <param name="Competencia">Competencia de referencia (<c>AAAA-MM</c>).</param>
/// <param name="TotalLiquido">Total liquido pago.</param>
/// <param name="DataPagamento">Data da liquidacao financeira.</param>
public sealed record PagamentoEfetuadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid FolhaDePagamentoId,
    string Competencia,
    decimal TotalLiquido,
    DateOnly DataPagamento) : IntegrationEvent(EventId, OccurredOnUtc);
