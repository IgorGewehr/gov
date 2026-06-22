using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Votacoes;

/// <summary>Cancela uma votacao aberta (terminal, sem resultado — I-10).</summary>
/// <param name="VotacaoId">Votacao a cancelar.</param>
public sealed record CancelarVotacaoCommand(Guid VotacaoId) : ICommand;

/// <summary>Regras de validacao do cancelamento de votacao.</summary>
public sealed class CancelarVotacaoValidator : AbstractValidator<CancelarVotacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public CancelarVotacaoValidator()
    {
        RuleFor(comando => comando.VotacaoId).NotEmpty();
    }
}

/// <summary>Handler do cancelamento de votacao.</summary>
public sealed class CancelarVotacaoHandler(
    IVotacaoRepository votacoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarVotacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarVotacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var votacao = await votacoes.ObterPorIdAsync(new VotacaoId(request.VotacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Votacao nao encontrada.");

        votacao.Cancelar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
