using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Contracts;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

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

/// <summary>Handler da Matricula Inicial.</summary>
public sealed class MatricularAlunoHandler(
    IMatriculaRepository matriculas,
    ITurmaRepository turmas,
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

        // I-11: a enturmacao respeita a invariante da Turma (matriculados <= vagas).
        if (!await turmas.PossuiVagaAsync(turmaId, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Turma sem vaga.");
        }

        // I-3: aluno nao pode ter duas matriculas ativas conflitantes no mesmo periodo/turno.
        if (await matriculas.ExisteMatriculaAtivaConflitanteAsync(alunoId, request.DataReferencia, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Aluno ja possui matricula ativa conflitante.");
        }

        var matricula = Matricula.MatricularAluno(
            tenant.TenantId,
            alunoId,
            turmaId,
            new EscolaId(request.EscolaId),
            request.DataReferencia);

        matriculas.Adicionar(matricula);
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
