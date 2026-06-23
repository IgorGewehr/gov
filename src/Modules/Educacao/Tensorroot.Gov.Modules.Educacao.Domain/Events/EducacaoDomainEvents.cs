using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Events;

/// <summary>Aluno matriculado (Matricula Inicial) — vinculo aluno-turma-escola criado em situacao Ativa.</summary>
/// <param name="MatriculaId">Identificador da matricula.</param>
/// <param name="AlunoId">Aluno vinculado.</param>
/// <param name="TurmaId">Turma de enturmacao.</param>
/// <param name="EscolaId">Escola da matricula.</param>
public sealed record AlunoMatriculado(MatriculaId MatriculaId, AlunoId AlunoId, TurmaId TurmaId, EscolaId EscolaId) : IDomainEvent;

/// <summary>Aluno transferido — a matricula passou ao estado terminal Transferida.</summary>
/// <param name="MatriculaId">Identificador da matricula.</param>
public sealed record AlunoTransferido(MatriculaId MatriculaId) : IDomainEvent;

/// <summary>Matricula encerrada — a matricula passou a um estado terminal (Concluida ou Abandono).</summary>
/// <param name="MatriculaId">Identificador da matricula.</param>
public sealed record MatriculaEncerrada(MatriculaId MatriculaId) : IDomainEvent;
