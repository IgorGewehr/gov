using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Events;

/// <summary>Turma criada (situacao inicial <see cref="SituacaoTurma.Planejada"/>). Emitida por <c>Turma.Criar</c>.</summary>
/// <param name="TurmaId">Identificador da turma criada.</param>
public sealed record TurmaCriada(TurmaId TurmaId) : IDomainEvent;

/// <summary>Turma aberta a enturmacao (<see cref="SituacaoTurma.Aberta"/>). Emitida por <c>Turma.Abrir</c>.</summary>
/// <param name="TurmaId">Identificador da turma aberta.</param>
public sealed record TurmaAberta(TurmaId TurmaId) : IDomainEvent;

/// <summary>Turma encerrada (<see cref="SituacaoTurma.Encerrada"/>). Emitida por <c>Turma.Encerrar</c>.</summary>
/// <param name="TurmaId">Identificador da turma encerrada.</param>
public sealed record TurmaEncerrada(TurmaId TurmaId) : IDomainEvent;
