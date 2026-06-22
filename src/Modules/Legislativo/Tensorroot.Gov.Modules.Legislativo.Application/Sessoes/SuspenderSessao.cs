using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;

/// <summary>Suspende temporariamente uma sessao Aberta.</summary>
/// <param name="SessaoId">Sessao a suspender.</param>
public sealed record SuspenderSessaoCommand(Guid SessaoId) : ICommand;

/// <summary>Regras de validacao da suspensao de sessao.</summary>
public sealed class SuspenderSessaoValidator : AbstractValidator<SuspenderSessaoCommand>
{
    /// <summary>Define as regras.</summary>
    public SuspenderSessaoValidator()
    {
        RuleFor(comando => comando.SessaoId).NotEmpty();
    }
}

/// <summary>Handler da suspensao de sessao.</summary>
public sealed class SuspenderSessaoHandler(
    ISessaoRepository sessoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SuspenderSessaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(SuspenderSessaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessao = await sessoes.ObterPorIdAsync(new SessaoId(request.SessaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sessao nao encontrada.");

        sessao.Suspender();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
