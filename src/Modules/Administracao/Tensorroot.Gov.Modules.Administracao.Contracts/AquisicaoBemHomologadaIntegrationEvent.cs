using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Administracao.Contracts;

/// <summary>
/// Evento de integracao publico: a homologacao de uma licitacao corresponde a aquisicao de bem
/// permanente, sinalizando ao Patrimonio o tombamento do bem adquirido. Consumivel por outros
/// Bounded Contexts. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="LicitacaoId">Identificador da licitacao homologada.</param>
/// <param name="Objeto">Descricao do objeto licitado.</param>
/// <param name="Valor">Valor adjudicado da aquisicao.</param>
public sealed record AquisicaoBemHomologadaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid LicitacaoId,
    string Objeto,
    decimal Valor) : IntegrationEvent(EventId, OccurredOnUtc);
