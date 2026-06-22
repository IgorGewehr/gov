using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Integracoes;

/// <summary>
/// Contrato (DTO) do evento de integracao publicado por modulos originadores (Licitacoes, RH/Ferias,
/// Licencas) para solicitar a autuacao de um processo administrativo, preservando a origem para a
/// devolucao do NUP. Definido localmente como Anti-Corruption Layer enquanto os modulos de origem
/// (e seus respectivos <c>Contracts</c>) ainda nao foram gerados; ao serem gerados, esta definicao
/// passara a residir no <c>Contracts</c> do modulo de origem. Idempotente por <c>EventId</c> no consumo.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="OrigemModulo">Modulo originador (ex.: "Licitacao", "RH", "Licencas").</param>
/// <param name="OrigemId">Identificador da origem no modulo originador.</param>
/// <param name="Classificacao">Classe documental sugerida (vincula a Tabela de Temporalidade).</param>
public sealed record AutuarProcessoRequested(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    string OrigemModulo,
    Guid OrigemId,
    string Classificacao) : IntegrationEvent(EventId, OccurredOnUtc);
