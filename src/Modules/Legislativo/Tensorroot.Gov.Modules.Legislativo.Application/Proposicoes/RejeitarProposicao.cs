using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;

/// <summary>Rejeita uma proposicao em Ordem do Dia conforme o resultado de uma votacao.</summary>
/// <param name="ProposicaoId">Proposicao a rejeitar.</param>
/// <param name="VotacaoId">Votacao cujo resultado rejeita a materia.</param>
public sealed record RejeitarProposicaoCommand(Guid ProposicaoId, Guid VotacaoId) : ICommand;

/// <summary>Regras de validacao da rejeicao de proposicao.</summary>
public sealed class RejeitarProposicaoValidator : AbstractValidator<RejeitarProposicaoCommand>
{
    /// <summary>Define as regras.</summary>
    public RejeitarProposicaoValidator()
    {
        RuleFor(comando => comando.ProposicaoId).NotEmpty();
        RuleFor(comando => comando.VotacaoId).NotEmpty();
    }
}

/// <summary>Handler da rejeicao de proposicao.</summary>
public sealed class RejeitarProposicaoHandler(
    IProposicaoRepository proposicoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RejeitarProposicaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RejeitarProposicaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var proposicao = await proposicoes.ObterPorIdAsync(new ProposicaoId(request.ProposicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Proposicao nao encontrada.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        proposicao.Rejeitar(hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
