using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;

namespace Tensorroot.Gov.Modules.Educacao.Application.Alunos;

/// <summary>Adiciona um responsavel a um aluno (LGPD art. 14), retornando o identificador do responsavel.</summary>
/// <param name="AlunoId">Identificador do aluno.</param>
/// <param name="Responsavel">Dados do responsavel a adicionar.</param>
public sealed record AdicionarResponsavelCommand(
    Guid AlunoId,
    ResponsavelPayload Responsavel) : ICommand<Guid>;

/// <summary>Regras de validacao da adicao de responsavel.</summary>
public sealed class AdicionarResponsavelValidator : AbstractValidator<AdicionarResponsavelCommand>
{
    /// <summary>Define as regras.</summary>
    public AdicionarResponsavelValidator()
    {
        RuleFor(comando => comando.AlunoId).NotEmpty();
        RuleFor(comando => comando.Responsavel).NotNull();
        RuleFor(comando => comando.Responsavel.Nome).NotEmpty().WithMessage("Nome do responsavel obrigatorio.");
        RuleFor(comando => comando.Responsavel.Parentesco).IsInEnum();
    }
}

/// <summary>Handler da adicao de responsavel.</summary>
public sealed class AdicionarResponsavelHandler(
    IAlunoRepository alunos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdicionarResponsavelCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AdicionarResponsavelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var aluno = await alunos.ObterPorIdAsync(new AlunoId(request.AlunoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Aluno nao encontrado.");

        var responsavel = AlunoMapeamento.CriarResponsavel(request.Responsavel);
        aluno.AdicionarResponsavel(responsavel);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return responsavel.Id.Value;
    }
}
