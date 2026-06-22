using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Legislativo.Contracts;

/// <summary>
/// Evento de integracao publico: uma edicao do Diario Oficial Eletronico foi publicada (marco legal
/// de eficacia dos atos). Publicado via Outbox (consistencia transacional com o estado), consumivel
/// pelo modulo Transparencia para indexar/expor no Portal LAI. Idempotente por <c>EventId</c> no
/// consumidor — segue o padrao de <see cref="AutografoEnviadoIntegrationEvent"/>.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (Camara) dono do registro.</param>
/// <param name="EdicaoDiarioId">Identificador da edicao publicada.</param>
/// <param name="Numero">Numero da edicao.</param>
/// <param name="Ano">Ano da edicao.</param>
/// <param name="DataPublicacao">Momento oficial da publicacao.</param>
public sealed record DiarioOficialPublicadoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid EdicaoDiarioId,
    int Numero,
    int Ano,
    DateTimeOffset DataPublicacao) : IntegrationEvent(EventId, OccurredOnUtc);
