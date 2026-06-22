using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Saude.Domain.Events;

/// <summary>Atendimento clinico registrado no PEP (situacao inicial EmAndamento).</summary>
/// <param name="AtendimentoId">Identificador do atendimento.</param>
/// <param name="PacienteId">Paciente atendido (referencia por Id).</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES) do atendimento.</param>
public sealed record AtendimentoRegistrado(
    AtendimentoId AtendimentoId,
    PacienteId PacienteId,
    EstabelecimentoId EstabelecimentoId) : IDomainEvent;

/// <summary>Atendimento assinado em ICP-Brasil (NGS2) — torna-se imutavel (somente adendo).</summary>
/// <param name="AtendimentoId">Identificador do atendimento.</param>
public sealed record AtendimentoAssinado(AtendimentoId AtendimentoId) : IDomainEvent;

/// <summary>Adendo datado/reassinado registrado, complementando uma evolucao ja assinada (append-only).</summary>
/// <param name="AtendimentoId">Identificador do atendimento.</param>
/// <param name="EvolucaoReferenciadaId">Evolucao assinada complementada pelo adendo.</param>
public sealed record AdendoRegistrado(AtendimentoId AtendimentoId, Guid EvolucaoReferenciadaId) : IDomainEvent;

/// <summary>RES compartilhado e aceito na RNDS (Bundle FHIR R4 via mTLS + ICP-Brasil).</summary>
/// <param name="AtendimentoId">Identificador do atendimento.</param>
public sealed record RESCompartilhadoNaRNDS(AtendimentoId AtendimentoId) : IDomainEvent;

/// <summary>Producao do PEP lancada no SISAB para a competencia (Portaria 1.412/2013).</summary>
/// <param name="AtendimentoId">Identificador do atendimento.</param>
/// <param name="Competencia">Competencia (AAAA-MM) da producao.</param>
public sealed record PEPLancadoNoSISAB(AtendimentoId AtendimentoId, Competencia Competencia) : IDomainEvent;

/// <summary>Atendimento cancelado antes da assinatura (terminal).</summary>
/// <param name="AtendimentoId">Identificador do atendimento.</param>
/// <param name="Motivo">Motivo do cancelamento.</param>
public sealed record AtendimentoCancelado(AtendimentoId AtendimentoId, string Motivo) : IDomainEvent;
