using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;

/// <summary>Apresenta uma emenda pontual a uma proposicao em tramitacao.</summary>
/// <param name="ProposicaoId">Proposicao alvo.</param>
/// <param name="Texto">Texto da emenda.</param>
/// <param name="Autoria">Autoria da emenda.</param>
public sealed record ApresentarEmendaCommand(Guid ProposicaoId, string Texto, string Autoria) : ICommand<Guid>;

/// <summary>Regras de validacao da apresentacao de emenda.</summary>
public sealed class ApresentarEmendaValidator : AbstractValidator<ApresentarEmendaCommand>
{
    /// <summary>Define as regras.</summary>
    public ApresentarEmendaValidator()
    {
        RuleFor(comando => comando.ProposicaoId).NotEmpty();
        RuleFor(comando => comando.Texto).NotEmpty().MaximumLength(4000);
        RuleFor(comando => comando.Autoria).NotEmpty().MaximumLength(Domain.Proposicoes.Autoria.ComprimentoMaximo);
    }
}

/// <summary>Handler da apresentacao de emenda.</summary>
public sealed class ApresentarEmendaHandler(
    IProposicaoRepository proposicoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ApresentarEmendaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ApresentarEmendaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var proposicao = await proposicoes.ObterPorIdAsync(new ProposicaoId(request.ProposicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Proposicao nao encontrada.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var emendaId = proposicao.ApresentarEmenda(request.Texto, Autoria.De(request.Autoria), hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return emendaId.Value;
    }
}
