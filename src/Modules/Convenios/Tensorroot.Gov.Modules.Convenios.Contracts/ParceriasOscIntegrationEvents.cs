using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Convenios.Contracts;

/// <summary>
/// Evento de integracao publico: uma parceria-saida OSC foi celebrada (fluxo B — MROSC). Consumido por
/// Financas (reserva/empenho), Transparencia (LAI) e PainelGestor. Idempotente por <c>EventId</c>.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="OscCnpj">CNPJ da OSC.</param>
/// <param name="OscRazaoSocial">Razao social da OSC.</param>
/// <param name="TipoInstrumento">Tipo do instrumento ("TermoColaboracao", "TermoFomento", "AcordoCooperacao").</param>
/// <param name="ValorGlobal">Valor global da parceria (zero no Acordo de Cooperacao).</param>
/// <param name="VigenciaInicio">Inicio da vigencia.</param>
/// <param name="VigenciaFim">Fim da vigencia.</param>
public sealed record ParceriaOscCelebradaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ParceriaId,
    string OscCnpj,
    string OscRazaoSocial,
    string TipoInstrumento,
    decimal ValorGlobal,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim) : IntegrationEvent(EventId, OccurredOnUtc);

/// <summary>
/// Evento de integracao publico: uma parcela de repasse a OSC precisa ser empenhada (fluxo B). Consumido
/// por Financas (empenho-&gt;liquidacao-&gt;pagamento do repasse de saida).
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant.</param>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="NumeroParcela">Numero de ordem da parcela.</param>
/// <param name="Valor">Valor da parcela a empenhar.</param>
/// <param name="ClassificacaoSugerida">Classificacao orcamentaria sugerida (texto; opcional).</param>
public sealed record RepasseOscAEmpenharIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ParceriaId,
    int NumeroParcela,
    decimal Valor,
    string? ClassificacaoSugerida) : IntegrationEvent(EventId, OccurredOnUtc);

/// <summary>
/// Evento de integracao publico: a analise da PC da OSC foi concluida (fluxo B). Consumido por Transparencia
/// (LAI) e PainelGestor.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant.</param>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="Resultado">Resultado ("Aprovada", "AprovadaComRessalva" ou "Rejeitada").</param>
public sealed record PrestacaoContasOscAnaliseConcluidaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ParceriaId,
    string Resultado) : IntegrationEvent(EventId, OccurredOnUtc);

/// <summary>
/// Evento de integracao publico: o prazo de analise da PC da OSC venceu sem decisao (fluxo B). Consumido por
/// PainelGestor (alerta). NAO aprova por decurso.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant.</param>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="PrazoVencido">Prazo de analise vencido.</param>
public sealed record PrazoAnaliseParceriaVencidoIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ParceriaId,
    DateOnly PrazoVencido) : IntegrationEvent(EventId, OccurredOnUtc);

/// <summary>
/// Evento de integracao publico: uma OSC foi declarada inadimplente (fluxo B — gatilho LRF art. 48).
/// Consumido por Financas e PainelGestor: BLOQUEIA novos repasses a esta OSC.
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant.</param>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="OscCnpj">CNPJ da OSC inadimplente.</param>
/// <param name="Motivo">Motivo da inadimplencia.</param>
public sealed record ParceriaOscInadimplenteIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid ParceriaId,
    string OscCnpj,
    string Motivo) : IntegrationEvent(EventId, OccurredOnUtc);
