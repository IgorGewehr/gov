using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Contracts;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Matriculas;

/// <summary>Matricula Inicial: vincula um aluno a uma turma de uma escola na data de referencia do Censo.</summary>
/// <param name="AlunoId">Aluno a matricular.</param>
/// <param name="TurmaId">Turma de enturmacao.</param>
/// <param name="EscolaId">Escola da matricula.</param>
/// <param name="DataReferencia">Data de referencia do Censo (Matricula Inicial).</param>
public sealed record MatricularAlunoCommand(
    Guid AlunoId,
    Guid TurmaId,
    Guid EscolaId,
    DateOnly DataReferencia) : ICommand<Guid>;

/// <summary>Regras de validacao da Matricula Inicial.</summary>
public sealed class MatricularAlunoValidator : AbstractValidator<MatricularAlunoCommand>
{
    /// <summary>Define as regras.</summary>
    public MatricularAlunoValidator()
    {
        RuleFor(comando => comando.AlunoId).NotEmpty().WithMessage("Aluno obrigatorio.");
        RuleFor(comando => comando.TurmaId).NotEmpty().WithMessage("Turma obrigatoria.");
        RuleFor(comando => comando.EscolaId).NotEmpty().WithMessage("Escola obrigatoria.");
        RuleFor(comando => comando.DataReferencia).NotEmpty().WithMessage("Data de referencia do Censo obrigatoria.");
    }
}

/// <summary>
/// Handler da Matricula Inicial (Onda 1 — ligacao a entidades reais). Carrega Aluno e Turma reais,
/// valida coerencia (aluno ativo; escola da turma == escola informada), cria a Matricula, incrementa
/// o contador da Turma e salva TUDO na MESMA transacao (mesmo modulo/contexto — nao viola isolamento).
/// </summary>
public sealed class MatricularAlunoHandler(
    IMatriculaRepository matriculas,
    ITurmaRepository turmas,
    IAlunoRepository alunos,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<MatricularAlunoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(MatricularAlunoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var turmaId = new TurmaId(request.TurmaId);
        var alunoId = new AlunoId(request.AlunoId);
        var escolaId = new EscolaId(request.EscolaId);

        // O aluno deve existir e estar Ativo.
        var aluno = await alunos.ObterPorIdAsync(alunoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Aluno nao encontrado.");
        if (!aluno.Ativo)
        {
            throw new InvalidOperationException("Aluno em situacao terminal nao pode ser matriculado.");
        }

        // A turma deve existir; carrega o agregado para validar vaga (I-T2) e coerencia de escola.
        var turma = await turmas.ObterPorIdAsync(turmaId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Turma nao encontrada.");

        // Coerencia aluno-turma-escola: a escola informada deve ser a da turma.
        if (turma.EscolaId != escolaId)
        {
            throw new InvalidOperationException("Escola informada diverge da escola da turma.");
        }

        // I-3: aluno nao pode ter duas matriculas ativas conflitantes na mesma data de referencia.
        if (await matriculas.ExisteMatriculaAtivaConflitanteAsync(alunoId, request.DataReferencia, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Aluno ja possui matricula ativa conflitante.");
        }

        // I-T2: a enturmacao incrementa o contador da Turma (valida turma Aberta com vaga).
        turma.IncrementarMatriculados();

        var matricula = Matricula.MatricularAluno(
            tenant.TenantId,
            alunoId,
            turmaId,
            escolaId,
            request.DataReferencia);

        matriculas.Adicionar(matricula);

        // Mesma Unit of Work: Matricula criada + contador da Turma atualizados atomicamente.
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new AlunoMatriculadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            matricula.Id.Value,
            matricula.AlunoId.Value,
            matricula.TurmaId.Value,
            matricula.EscolaId.Value);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);

        return matricula.Id.Value;
    }
}
