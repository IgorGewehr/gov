using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Educacao.Contracts;

/// <summary>
/// Evento de integracao publico: uma matricula foi encerrada (por conclusao ou abandono) no
/// modulo Educacao. Publicado a Transparencia e demais modulos. Consumivel por outros Bounded
/// Contexts. Idempotente por <c>EventId</c> no consumidor (reentrega via Outbox).
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente municipal/rede) dono do registro.</param>
/// <param name="MatriculaId">Identificador da matricula encerrada.</param>
/// <param name="Motivo">Motivo do encerramento (<c>Conclusao</c> ou <c>Abandono</c>).</param>
public sealed record MatriculaEncerradaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid MatriculaId,
    string Motivo) : IntegrationEvent(EventId, OccurredOnUtc);
