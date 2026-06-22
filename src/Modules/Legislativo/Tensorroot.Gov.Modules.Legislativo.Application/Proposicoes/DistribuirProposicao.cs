using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;

/// <summary>Distribui uma proposicao apresentada as Comissoes para instrucao.</summary>
/// <param name="ProposicaoId">Proposicao a distribuir.</param>
public sealed record DistribuirProposicaoCommand(Guid ProposicaoId) : ICommand;

/// <summary>Regras de validacao da distribuicao de proposicao.</summary>
public sealed class DistribuirProposicaoValidator : AbstractValidator<DistribuirProposicaoCommand>
{
    /// <summary>Define as regras.</summary>
    public DistribuirProposicaoValidator()
    {
        RuleFor(comando => comando.ProposicaoId).NotEmpty();
    }
}

/// <summary>Handler da distribuicao de proposicao.</summary>
public sealed class DistribuirProposicaoHandler(
    IProposicaoRepository proposicoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<DistribuirProposicaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(DistribuirProposicaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var proposicao = await proposicoes.ObterPorIdAsync(new ProposicaoId(request.ProposicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Proposicao nao encontrada.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        proposicao.Distribuir(hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
