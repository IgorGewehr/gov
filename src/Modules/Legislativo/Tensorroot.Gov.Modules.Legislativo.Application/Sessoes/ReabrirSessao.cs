using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;

/// <summary>Reabre uma sessao Suspensa.</summary>
/// <param name="SessaoId">Sessao a reabrir.</param>
public sealed record ReabrirSessaoCommand(Guid SessaoId) : ICommand;

/// <summary>Regras de validacao da reabertura de sessao.</summary>
public sealed class ReabrirSessaoValidator : AbstractValidator<ReabrirSessaoCommand>
{
    /// <summary>Define as regras.</summary>
    public ReabrirSessaoValidator()
    {
        RuleFor(comando => comando.SessaoId).NotEmpty();
    }
}

/// <summary>Handler da reabertura de sessao.</summary>
public sealed class ReabrirSessaoHandler(
    ISessaoRepository sessoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReabrirSessaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReabrirSessaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessao = await sessoes.ObterPorIdAsync(new SessaoId(request.SessaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sessao nao encontrada.");

        sessao.Reabrir();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
