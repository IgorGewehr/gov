using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Events;

/// <summary>Licitacao aberta: edital publicado e certame iniciado (situacao inicial Aberta).</summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
/// <param name="Modalidade">Modalidade do certame (art. 28/74/75).</param>
public sealed record LicitacaoAberta(LicitacaoId LicitacaoId, ModalidadeLicitacao Modalidade) : IDomainEvent;

/// <summary>Edital publicado no Portal Nacional de Contratacoes Publicas (art. 174).</summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
/// <param name="NumeroEditalPncp">Identificador da contratacao no PNCP.</param>
public sealed record EditalPublicadoNoPncp(LicitacaoId LicitacaoId, string NumeroEditalPncp) : IDomainEvent;

/// <summary>Licitacao homologada pela autoridade competente (art. 71).</summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
/// <param name="PropostaVencedoraId">Proposta julgada vencedora.</param>
/// <param name="FornecedorVencedorId">Fornecedor vencedor.</param>
public sealed record LicitacaoHomologada(
    LicitacaoId LicitacaoId,
    Guid PropostaVencedoraId,
    Guid FornecedorVencedorId) : IDomainEvent;

/// <summary>Licitacao declarada fracassada — sem proposta valida/habilitada (art. 71).</summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
/// <param name="Motivo">Motivacao do ato.</param>
public sealed record LicitacaoFracassada(LicitacaoId LicitacaoId, string Motivo) : IDomainEvent;

/// <summary>Licitacao declarada deserta — sem interessados (art. 71).</summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
public sealed record LicitacaoDeserta(LicitacaoId LicitacaoId) : IDomainEvent;

/// <summary>Licitacao revogada por conveniencia/oportunidade (art. 71).</summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
/// <param name="Motivo">Motivacao do ato administrativo.</param>
public sealed record LicitacaoRevogada(LicitacaoId LicitacaoId, string Motivo) : IDomainEvent;

/// <summary>Licitacao anulada por ilegalidade (art. 71).</summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
/// <param name="Motivo">Motivacao do ato administrativo (vicio de legalidade).</param>
public sealed record LicitacaoAnulada(LicitacaoId LicitacaoId, string Motivo) : IDomainEvent;
