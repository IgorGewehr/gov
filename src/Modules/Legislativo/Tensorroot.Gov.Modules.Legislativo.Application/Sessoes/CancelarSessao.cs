using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;

/// <summary>Cancela uma sessao Agendada (ex.: ausencia de quorum no horario) — terminal.</summary>
/// <param name="SessaoId">Sessao a cancelar.</param>
public sealed record CancelarSessaoCommand(Guid SessaoId) : ICommand;

/// <summary>Regras de validacao do cancelamento de sessao.</summary>
public sealed class CancelarSessaoValidator : AbstractValidator<CancelarSessaoCommand>
{
    /// <summary>Define as regras.</summary>
    public CancelarSessaoValidator()
    {
        RuleFor(comando => comando.SessaoId).NotEmpty();
    }
}

/// <summary>Handler do cancelamento de sessao.</summary>
public sealed class CancelarSessaoHandler(
    ISessaoRepository sessoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarSessaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarSessaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessao = await sessoes.ObterPorIdAsync(new SessaoId(request.SessaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sessao nao encontrada.");

        sessao.Cancelar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
