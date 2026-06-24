using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Events;

/// <summary>Convenio federal recebido registrado em proposta (fluxo A).</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
public sealed record ConvenioRecebidoRegistrado(ConvenioRecebidoId ConvenioId) : IDomainEvent;

/// <summary>Plano de trabalho aprovado pelo concedente (fluxo A).</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
public sealed record PlanoTrabalhoConvenioAprovado(ConvenioRecebidoId ConvenioId) : IDomainEvent;

/// <summary>Convenio federal celebrado (fluxo A) — gatilho da receita de convenio + reserva da contrapartida.</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="ValorGlobal">Valor global do plano.</param>
/// <param name="ValorContrapartida">Valor pactuado da contrapartida.</param>
public sealed record ConvenioRecebidoCelebrado(ConvenioRecebidoId ConvenioId, decimal ValorGlobal, decimal ValorContrapartida) : IDomainEvent;

/// <summary>Parcela de repasse liberada/recebida (fluxo A).</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="NumeroOrdem">Numero de ordem da parcela.</param>
/// <param name="Valor">Valor liberado.</param>
public sealed record ParcelaConvenioLiberada(ConvenioRecebidoId ConvenioId, int NumeroOrdem, decimal Valor) : IDomainEvent;

/// <summary>Contrapartida empenhada (fluxo A) — satisfaz A-INV-5; reage a evento de Financas.</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="ValorEmpenhado">Valor empenhado acumulado.</param>
public sealed record ContrapartidaConvenioEmpenhada(ConvenioRecebidoId ConvenioId, decimal ValorEmpenhado) : IDomainEvent;

/// <summary>Prestacao de contas (parcial/final) submetida ao convenio (fluxo A).</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="Tipo">Tipo da PC (parcial/final).</param>
/// <param name="DataSubmissao">Data de submissao.</param>
/// <param name="PrazoAnalise">Prazo legal de analise (calculado).</param>
public sealed record PrestacaoContasConvenioSubmetida(
    ConvenioRecebidoId ConvenioId,
    TipoPrestacaoConvenio Tipo,
    DateOnly DataSubmissao,
    DateOnly PrazoAnalise) : IDomainEvent;

/// <summary>Analise de uma PC do convenio concluida (fluxo A).</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="Tipo">Tipo da PC.</param>
/// <param name="Resultado">Resultado (aprovada/ressalva/rejeitada).</param>
public sealed record PrestacaoContasConvenioAnalisada(
    ConvenioRecebidoId ConvenioId,
    TipoPrestacaoConvenio Tipo,
    ResultadoAnalise Resultado) : IDomainEvent;

/// <summary>Convenio declarado inadimplente (fluxo A) — bloqueia liberacoes.</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="Motivo">Motivo da inadimplencia.</param>
public sealed record ConvenioRecebidoInadimplente(ConvenioRecebidoId ConvenioId, string Motivo) : IDomainEvent;
