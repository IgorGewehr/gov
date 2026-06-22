using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

namespace Tensorroot.Gov.Modules.Saude.Application.Regulacao;

/// <summary>Nega (indefere) uma solicitacao de regulacao em analise.</summary>
/// <param name="SolicitacaoRegulacaoId">Solicitacao a negar.</param>
/// <param name="Motivo">Motivo da negativa.</param>
public sealed record NegarSolicitacaoRegulacaoCommand(Guid SolicitacaoRegulacaoId, string Motivo) : ICommand;

/// <summary>Regras de validacao da negativa de solicitacao de regulacao.</summary>
public sealed class NegarSolicitacaoRegulacaoValidator : AbstractValidator<NegarSolicitacaoRegulacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public NegarSolicitacaoRegulacaoValidator()
    {
        RuleFor(comando => comando.SolicitacaoRegulacaoId).NotEmpty().WithMessage("Solicitacao e obrigatoria.");
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(1000).WithMessage("Motivo da negativa e obrigatorio (max. 1000).");
    }
}

/// <summary>Handler da negativa de solicitacao de regulacao.</summary>
public sealed class NegarSolicitacaoRegulacaoHandler(
    ISolicitacaoRegulacaoRepository solicitacoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<NegarSolicitacaoRegulacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(NegarSolicitacaoRegulacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var solicitacao = await solicitacoes
            .ObterPorIdAsync(new SolicitacaoRegulacaoId(request.SolicitacaoRegulacaoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Solicitacao nao encontrada.");

        solicitacao.Negar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
