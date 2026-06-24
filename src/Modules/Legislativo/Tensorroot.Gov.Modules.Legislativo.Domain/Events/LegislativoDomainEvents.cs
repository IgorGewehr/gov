using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Events;

/// <summary>Vereador cadastrado (mandato em exercicio) — emitido por <c>Vereador.Cadastrar</c> (V-1).</summary>
/// <param name="VereadorId">Identificador (reusa o id ja referenciado por presencas/votos).</param>
/// <param name="NomeParlamentar">Nome parlamentar do vereador.</param>
/// <param name="Partido">Sigla partidaria.</param>
public sealed record VereadorCadastrado(VereadorId VereadorId, string NomeParlamentar, string Partido) : IDomainEvent;

/// <summary>Proposicao apresentada/protocolada (situacao inicial <see cref="SituacaoProposicao.Apresentada"/>).</summary>
/// <param name="ProposicaoId">Identificador da proposicao.</param>
/// <param name="Tipo">Especie da materia apresentada.</param>
/// <param name="Autoria">Autoria (iniciativa) da proposicao.</param>
public sealed record ProposicaoApresentada(ProposicaoId ProposicaoId, TipoProposicao Tipo, Autoria Autoria) : IDomainEvent;

/// <summary>Proposicao distribuida as Comissoes para instrucao.</summary>
/// <param name="ProposicaoId">Identificador da proposicao.</param>
public sealed record ProposicaoDistribuida(ProposicaoId ProposicaoId) : IDomainEvent;

/// <summary>Emenda (ou substitutivo) apresentado a proposicao em curso.</summary>
/// <param name="ProposicaoId">Identificador da proposicao.</param>
/// <param name="EmendaId">Identificador da emenda apresentada.</param>
public sealed record EmendaApresentada(ProposicaoId ProposicaoId, EmendaId EmendaId) : IDomainEvent;

/// <summary>Parecer de Comissao emitido sobre a materia (registrado na trilha imutavel).</summary>
/// <param name="ProposicaoId">Identificador da proposicao.</param>
/// <param name="Comissao">Comissao emitente (ex.: Ccj, FinancasOrcamento).</param>
/// <param name="Favoravel">Sentido do parecer (favoravel/contrario).</param>
public sealed record ParecerEmitido(ProposicaoId ProposicaoId, string Comissao, bool Favoravel) : IDomainEvent;

/// <summary>
/// Um turno de votacao de materia de rito qualificado (Emenda a LOM) foi aprovado, mas a materia
/// AINDA NAO esta aprovada — faltam turnos. Distingue-se de <see cref="ProposicaoAprovada"/>, que so
/// e emitido quando TODOS os turnos exigidos sao concluidos.
/// </summary>
/// <param name="ProposicaoId">Identificador da proposicao.</param>
/// <param name="Turno">Numero do turno aprovado (1 ou 2).</param>
/// <param name="TurnosExigidos">Total de turnos exigidos pelo tipo da materia.</param>
public sealed record TurnoAprovado(ProposicaoId ProposicaoId, int Turno, int TurnosExigidos) : IDomainEvent;

/// <summary>Proposicao aprovada em Plenario (todos os turnos exigidos concluidos).</summary>
/// <param name="ProposicaoId">Identificador da proposicao.</param>
public sealed record ProposicaoAprovada(ProposicaoId ProposicaoId) : IDomainEvent;

/// <summary>Proposicao rejeitada em Plenario (terminal).</summary>
/// <param name="ProposicaoId">Identificador da proposicao.</param>
public sealed record ProposicaoRejeitada(ProposicaoId ProposicaoId) : IDomainEvent;

/// <summary>Proposicao arquivada (terminal).</summary>
/// <param name="ProposicaoId">Identificador da proposicao.</param>
public sealed record ProposicaoArquivada(ProposicaoId ProposicaoId) : IDomainEvent;

/// <summary>Autografo gerado e enviado ao Executivo (mapeia para o Integration Event via Outbox).</summary>
/// <param name="ProposicaoId">Identificador da proposicao.</param>
/// <param name="NumeroAutografo">Numero do autografo gerado.</param>
public sealed record AutografoEnviado(ProposicaoId ProposicaoId, string NumeroAutografo) : IDomainEvent;

/// <summary>Quorum de instalacao apurado para uma sessao (I-4).</summary>
/// <param name="SessaoId">Identificador da sessao.</param>
/// <param name="Atingido">Indica se o quorum de instalacao foi atingido.</param>
public sealed record QuorumVerificado(SessaoId SessaoId, bool Atingido) : IDomainEvent;

/// <summary>Sessao instalada/aberta (quorum atingido) — I-5.</summary>
/// <param name="SessaoId">Identificador da sessao aberta.</param>
public sealed record SessaoAberta(SessaoId SessaoId) : IDomainEvent;

/// <summary>Sessao encerrada (terminal) — I-8.</summary>
/// <param name="SessaoId">Identificador da sessao encerrada.</param>
public sealed record SessaoEncerrada(SessaoId SessaoId) : IDomainEvent;
