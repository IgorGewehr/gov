using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Patrimonio.Contracts;

/// <summary>
/// Evento de integração público: um inventário (Lei 4.320 art. 96) foi encerrado, apurando as
/// divergências (falta/sobra/localização/estado/valor) e respectivas recomendações de efetivação
/// (baixa/transferência/incorporação/reavaliação). Consumível por outros Bounded Contexts (ex.: Finanças,
/// Transparência/TCE). Idempotente por <c>EventId</c> no consumidor; publicado via Outbox.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="InventarioId">Identificador do inventário encerrado.</param>
/// <param name="Exercicio">Exercício (ano-base) do levantamento.</param>
/// <param name="TotalDivergencias">Quantidade total de divergências apuradas.</param>
public sealed record InventarioEncerradoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid InventarioId,
    int Exercicio,
    int TotalDivergencias) : IntegrationEvent(EventId, OccurredOnUtc);
