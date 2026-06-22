using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Contracts;

/// <summary>
/// Evento de integracao publico: a folha de pagamento de uma competencia foi fechada, permitindo
/// que Contabilidade/Empenho reconheca a despesa de pessoal (Lei 4.320). Consumivel por outros
/// Bounded Contexts. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="FolhaDePagamentoId">Identificador da folha fechada.</param>
/// <param name="Competencia">Competencia de referencia (<c>AAAA-MM</c>).</param>
/// <param name="TotalLiquido">Total liquido apurado na folha.</param>
public sealed record FolhaFechadaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid FolhaDePagamentoId,
    string Competencia,
    decimal TotalLiquido) : IntegrationEvent(EventId, OccurredOnUtc);
