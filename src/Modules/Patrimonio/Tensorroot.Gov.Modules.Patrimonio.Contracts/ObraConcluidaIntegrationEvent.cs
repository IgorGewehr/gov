using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Patrimonio.Contracts;

/// <summary>
/// Evento de integração público: uma obra foi concluída fisicamente e incorporada ao acervo como bem
/// patrimonial (imobilizado — MCASP, variação patrimonial aumentativa). A incorporação contábil em
/// Finanças se dá pelo <c>BemIncorporadoIntegrationEvent</c> (in-process → bem); este evento notifica o
/// Portal do Gestor/Transparência da conclusão. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrência.</param>
/// <param name="TenantId">Tenant (ente público) dono do registro.</param>
/// <param name="ObraId">Identificador da obra concluída.</param>
/// <param name="ContratoId">Contrato NLLC de origem.</param>
/// <param name="BemPatrimonialId">Bem patrimonial criado pela incorporação.</param>
/// <param name="ValorFinal">Valor final (medido acumulado) — custo do imobilizado.</param>
/// <param name="DataConclusao">Data de conclusão da obra (base do relógio art. 94 §3 — 45 d.u.).</param>
public sealed record ObraConcluidaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ObraId,
    Guid ContratoId,
    Guid BemPatrimonialId,
    decimal ValorFinal,
    DateOnly DataConclusao) : IntegrationEvent(EventId, OccurredOnUtc);
