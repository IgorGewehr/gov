using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;

namespace Tensorroot.Gov.Modules.Educacao.Application.Alunos;

/// <summary>Inativa o cadastro de um aluno (estado terminal — obito, evasao definitiva, duplicidade).</summary>
/// <param name="AlunoId">Identificador do aluno.</param>
/// <param name="Motivo">Motivo da inativacao (obrigatorio).</param>
public sealed record InativarAlunoCommand(Guid AlunoId, string Motivo) : ICommand;

/// <summary>Regras de validacao da inativacao de aluno.</summary>
public sealed class InativarAlunoValidator : AbstractValidator<InativarAlunoCommand>
{
    /// <summary>Define as regras.</summary>
    public InativarAlunoValidator()
    {
        RuleFor(comando => comando.AlunoId).NotEmpty();
        RuleFor(comando => comando.Motivo).NotEmpty().WithMessage("Motivo da inativacao obrigatorio.");
    }
}

/// <summary>Handler da inativacao de aluno.</summary>
public sealed class InativarAlunoHandler(
    IAlunoRepository alunos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<InativarAlunoCommand>
{
    /// <inheritdoc />
    public async Task Handle(InativarAlunoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var aluno = await alunos.ObterPorIdAsync(new AlunoId(request.AlunoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Aluno nao encontrado.");

        aluno.Inativar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
