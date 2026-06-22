using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;

/// <summary>Inclui uma proposicao instruida em Ordem do Dia para deliberacao.</summary>
/// <param name="ProposicaoId">Proposicao a incluir.</param>
public sealed record IncluirEmOrdemDoDiaCommand(Guid ProposicaoId) : ICommand;

/// <summary>Regras de validacao da inclusao em Ordem do Dia.</summary>
public sealed class IncluirEmOrdemDoDiaValidator : AbstractValidator<IncluirEmOrdemDoDiaCommand>
{
    /// <summary>Define as regras.</summary>
    public IncluirEmOrdemDoDiaValidator()
    {
        RuleFor(comando => comando.ProposicaoId).NotEmpty();
    }
}

/// <summary>Handler da inclusao em Ordem do Dia.</summary>
public sealed class IncluirEmOrdemDoDiaHandler(
    IProposicaoRepository proposicoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<IncluirEmOrdemDoDiaCommand>
{
    /// <inheritdoc />
    public async Task Handle(IncluirEmOrdemDoDiaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var proposicao = await proposicoes.ObterPorIdAsync(new ProposicaoId(request.ProposicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Proposicao nao encontrada.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        proposicao.IncluirEmOrdemDoDia(hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
