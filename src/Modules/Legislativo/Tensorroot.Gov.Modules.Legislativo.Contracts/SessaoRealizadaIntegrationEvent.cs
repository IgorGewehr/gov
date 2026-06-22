using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Legislativo.Contracts;

/// <summary>
/// Evento de integracao publico: uma sessao plenaria foi realizada (instalada/aberta ou
/// encerrada) no modulo Legislativo. Consumivel por outros Bounded Contexts e pelo portal de
/// transparencia (LAI / APIs abertas). Idempotente por <c>EventId</c> no consumo.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (Camara Municipal) dono do registro.</param>
/// <param name="SessaoId">Identificador da sessao.</param>
/// <param name="Tipo">Especie da sessao (Ordinaria / Extraordinaria).</param>
/// <param name="DataHora">Momento agendado/realizado da sessao.</param>
public sealed record SessaoRealizadaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid SessaoId,
    string Tipo,
    DateTimeOffset DataHora) : IntegrationEvent(EventId, OccurredOnUtc);
