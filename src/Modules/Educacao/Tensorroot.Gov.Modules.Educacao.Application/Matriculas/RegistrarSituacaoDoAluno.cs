using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

namespace Tensorroot.Gov.Modules.Educacao.Application.Matriculas;

/// <summary>Registra a Situacao do Aluno (2a etapa do Censo — rendimento + movimento) sem alterar a situacao da matricula.</summary>
/// <param name="MatriculaId">Identificador da matricula.</param>
/// <param name="Rendimento">Rendimento do aluno (aprovado/reprovado).</param>
/// <param name="Movimento">Movimento do aluno (sem movimento/transferido/abandono/falecido).</param>
public sealed record RegistrarSituacaoDoAlunoCommand(
    Guid MatriculaId,
    Rendimento Rendimento,
    Movimento Movimento) : ICommand;

/// <summary>Regras de validacao do registro da Situacao do Aluno.</summary>
public sealed class RegistrarSituacaoDoAlunoValidator : AbstractValidator<RegistrarSituacaoDoAlunoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarSituacaoDoAlunoValidator()
    {
        RuleFor(comando => comando.MatriculaId).NotEmpty().WithMessage("Identificador da matricula obrigatorio.");
        RuleFor(comando => comando.Rendimento).IsInEnum().WithMessage("Rendimento invalido.");
        RuleFor(comando => comando.Movimento).IsInEnum().WithMessage("Movimento invalido.");
    }
}

/// <summary>Handler do registro da Situacao do Aluno.</summary>
public sealed class RegistrarSituacaoDoAlunoHandler(
    IMatriculaRepository matriculas,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarSituacaoDoAlunoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarSituacaoDoAlunoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var matricula = await matriculas.ObterPorIdAsync(new MatriculaId(request.MatriculaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Matricula nao encontrada.");

        // Regra de dominio: exige situacao Ativa (I-5); pre-requisito do encerramento do ano letivo (I-10).
        matricula.RegistrarSituacaoDoAluno(request.Rendimento, request.Movimento);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
