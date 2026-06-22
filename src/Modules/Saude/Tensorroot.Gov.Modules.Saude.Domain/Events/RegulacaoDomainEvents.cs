using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Saude.Domain.Events;

/// <summary>Solicitacao de regulacao aberta (estado inicial <c>Solicitada</c>).</summary>
/// <param name="SolicitacaoRegulacaoId">Identificador da solicitacao.</param>
/// <param name="PacienteId">Paciente da solicitacao.</param>
/// <param name="Procedimento">Procedimento SIGTAP solicitado.</param>
public sealed record RegulacaoSolicitada(
    SolicitacaoRegulacaoId SolicitacaoRegulacaoId,
    PacienteId PacienteId,
    Procedimento Procedimento) : IDomainEvent;

/// <summary>Solicitacao autorizada pelo regulador; vaga reservada no SISREG e cota consumida.</summary>
/// <param name="SolicitacaoRegulacaoId">Identificador da solicitacao.</param>
/// <param name="ProtocoloSisreg">Protocolo da reserva no SISREG.</param>
public sealed record SolicitacaoAutorizada(
    SolicitacaoRegulacaoId SolicitacaoRegulacaoId,
    string ProtocoloSisreg) : IDomainEvent;

/// <summary>Solicitacao negada (indeferida) pelo regulador.</summary>
/// <param name="SolicitacaoRegulacaoId">Identificador da solicitacao.</param>
/// <param name="Motivo">Motivo da negativa.</param>
public sealed record SolicitacaoNegada(
    SolicitacaoRegulacaoId SolicitacaoRegulacaoId,
    string Motivo) : IDomainEvent;

/// <summary>Solicitacao devolvida ao solicitante para complementacao/ajuste.</summary>
/// <param name="SolicitacaoRegulacaoId">Identificador da solicitacao.</param>
/// <param name="Motivo">Motivo da devolucao.</param>
public sealed record SolicitacaoDevolvida(
    SolicitacaoRegulacaoId SolicitacaoRegulacaoId,
    string Motivo) : IDomainEvent;

/// <summary>Procedimento autorizado executado (realizado/atendido).</summary>
/// <param name="SolicitacaoRegulacaoId">Identificador da solicitacao.</param>
public sealed record ProcedimentoExecutado(
    SolicitacaoRegulacaoId SolicitacaoRegulacaoId) : IDomainEvent;

/// <summary>Solicitacao cancelada pelo solicitante.</summary>
/// <param name="SolicitacaoRegulacaoId">Identificador da solicitacao.</param>
/// <param name="Motivo">Motivo do cancelamento.</param>
public sealed record SolicitacaoCancelada(
    SolicitacaoRegulacaoId SolicitacaoRegulacaoId,
    string Motivo) : IDomainEvent;
