using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;

/// <summary>Arquiva uma proposicao nao terminal (ex.: ao fim da legislatura).</summary>
/// <param name="ProposicaoId">Proposicao a arquivar.</param>
public sealed record ArquivarProposicaoCommand(Guid ProposicaoId) : ICommand;

/// <summary>Regras de validacao do arquivamento de proposicao.</summary>
public sealed class ArquivarProposicaoValidator : AbstractValidator<ArquivarProposicaoCommand>
{
    /// <summary>Define as regras.</summary>
    public ArquivarProposicaoValidator()
    {
        RuleFor(comando => comando.ProposicaoId).NotEmpty();
    }
}

/// <summary>Handler do arquivamento de proposicao.</summary>
public sealed class ArquivarProposicaoHandler(
    IProposicaoRepository proposicoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ArquivarProposicaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ArquivarProposicaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var proposicao = await proposicoes.ObterPorIdAsync(new ProposicaoId(request.ProposicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Proposicao nao encontrada.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        proposicao.Arquivar(hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
