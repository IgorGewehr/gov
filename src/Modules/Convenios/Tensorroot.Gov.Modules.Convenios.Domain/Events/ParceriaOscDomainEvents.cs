using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Events;

/// <summary>Parceria-saida OSC iniciada em selecao (fluxo B).</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="TipoInstrumento">Tipo de instrumento (colaboracao/fomento/cooperacao).</param>
public sealed record ParceriaOscIniciada(ParceriaOscId ParceriaId, TipoInstrumentoMrosc TipoInstrumento) : IDomainEvent;

/// <summary>Plano de trabalho da parceria aprovado (fluxo B).</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
public sealed record PlanoTrabalhoOscAprovado(ParceriaOscId ParceriaId) : IDomainEvent;

/// <summary>Parceria-saida OSC celebrada (fluxo B) — gatilho da reserva/empenho de repasse.</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="ValorGlobal">Valor global da parceria.</param>
public sealed record ParceriaOscCelebrada(ParceriaOscId ParceriaId, decimal ValorGlobal) : IDomainEvent;

/// <summary>Repasse a OSC liberado (fluxo B) — espelha execucao orcamentaria de saida.</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="NumeroOrdem">Numero de ordem da parcela.</param>
/// <param name="Valor">Valor repassado.</param>
public sealed record RepasseOscLiberado(ParceriaOscId ParceriaId, int NumeroOrdem, decimal Valor) : IDomainEvent;

/// <summary>PC da OSC recebida (fluxo B).</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="DataRecebimento">Data de recebimento.</param>
/// <param name="PrazoAnalise">Prazo legal de analise (calculado, 150 d).</param>
public sealed record PrestacaoContasOscRecebida(ParceriaOscId ParceriaId, DateOnly DataRecebimento, DateOnly PrazoAnalise) : IDomainEvent;

/// <summary>Analise da PC da OSC concluida (fluxo B).</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="Resultado">Resultado (aprovada/ressalva/rejeitada).</param>
public sealed record PrestacaoContasOscAnalisada(ParceriaOscId ParceriaId, ResultadoAnalise Resultado) : IDomainEvent;

/// <summary>OSC declarada inadimplente (fluxo B) — gatilho LRF: BLOQUEIA novos repasses.</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="Motivo">Motivo da inadimplencia.</param>
public sealed record ParceriaOscInadimplente(ParceriaOscId ParceriaId, string Motivo) : IDomainEvent;
