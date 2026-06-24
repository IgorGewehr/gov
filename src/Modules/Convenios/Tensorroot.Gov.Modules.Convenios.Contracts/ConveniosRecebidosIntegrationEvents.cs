using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Convenios.Contracts;

/// <summary>
/// Evento de integracao publico: um convenio federal RECEBIDO foi celebrado (fluxo A — Dec. 11.531/2023).
/// Consumido por Financas (reconhece a RECEITA de convenio + reserva da contrapartida), Transparencia (LAI)
/// e PainelGestor. Idempotente por <c>EventId</c> no consumidor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="ConcedenteCnpj">CNPJ do orgao concedente.</param>
/// <param name="ConcedenteNome">Nome do orgao concedente.</param>
/// <param name="NumeroConvenioTransferegov">Numero do convenio no Transferegov.br.</param>
/// <param name="ValorGlobal">Valor global do plano (repasse + contrapartida).</param>
/// <param name="ValorContrapartida">Valor pactuado da contrapartida.</param>
/// <param name="VigenciaInicio">Inicio da vigencia.</param>
/// <param name="VigenciaFim">Fim da vigencia.</param>
public sealed record ConvenioRecebidoCelebradoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ConvenioId,
    string ConcedenteCnpj,
    string ConcedenteNome,
    string NumeroConvenioTransferegov,
    decimal ValorGlobal,
    decimal ValorContrapartida,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim) : IntegrationEvent(EventId, OccurredOnUtc);

/// <summary>
/// Evento de integracao publico: a contrapartida de um convenio recebido precisa ser empenhada (fluxo A).
/// Consumido por Financas (gera o empenho da contrapartida). A <see cref="ClassificacaoSugerida"/> orienta a
/// dotacao; Financas e a autoridade da classificacao final.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant.</param>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="ValorPactuado">Valor pactuado da contrapartida a empenhar.</param>
/// <param name="ClassificacaoSugerida">Classificacao orcamentaria sugerida (texto; opcional).</param>
public sealed record ContrapartidaConvenioAEmpenharIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ConvenioId,
    decimal ValorPactuado,
    string? ClassificacaoSugerida) : IntegrationEvent(EventId, OccurredOnUtc);

/// <summary>
/// Evento de integracao publico: uma PC (parcial/final) de convenio recebido foi submetida (fluxo A).
/// Consumido por Transparencia (LAI) e PainelGestor (acompanhamento de prazos).
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant.</param>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="Tipo">Tipo da PC ("Parcial" ou "Final").</param>
/// <param name="DataSubmissao">Data de submissao.</param>
/// <param name="PrazoAnalise">Prazo legal de analise (calculado).</param>
public sealed record PrestacaoContasConvenioSubmetidaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ConvenioId,
    string Tipo,
    DateOnly DataSubmissao,
    DateOnly PrazoAnalise) : IntegrationEvent(EventId, OccurredOnUtc);

/// <summary>
/// Evento de integracao publico: a analise de uma PC de convenio recebido foi concluida (fluxo A).
/// Consumido por Transparencia (LAI) e PainelGestor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant.</param>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="Tipo">Tipo da PC ("Parcial" ou "Final").</param>
/// <param name="Resultado">Resultado ("Aprovada", "AprovadaComRessalva" ou "Rejeitada").</param>
public sealed record PrestacaoContasConvenioAnaliseConcluidaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ConvenioId,
    string Tipo,
    string Resultado) : IntegrationEvent(EventId, OccurredOnUtc);

/// <summary>
/// Evento de integracao publico: o prazo de analise de uma PC de convenio venceu sem decisao (fluxo A).
/// Consumido por PainelGestor (alerta). Emitido por uma varredura de prazos (job).
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant.</param>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="PrazoVencido">Prazo de analise vencido.</param>
public sealed record PrazoAnaliseConvenioVencidoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ConvenioId,
    DateOnly PrazoVencido) : IntegrationEvent(EventId, OccurredOnUtc);

/// <summary>
/// Evento de integracao publico: um convenio recebido foi declarado inadimplente (fluxo A — gatilho LRF).
/// Consumido por Financas e PainelGestor (nao libera nova parcela).
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant.</param>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="Motivo">Motivo da inadimplencia.</param>
public sealed record ConvenioInadimplenteIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ConvenioId,
    string Motivo) : IntegrationEvent(EventId, OccurredOnUtc);
