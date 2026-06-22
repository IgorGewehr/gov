using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

namespace Tensorroot.Gov.Modules.Saude.Application.Regulacao;

/// <summary>Devolve uma solicitacao de regulacao ao solicitante para complementacao/ajuste.</summary>
/// <param name="SolicitacaoRegulacaoId">Solicitacao a devolver.</param>
/// <param name="Motivo">Motivo da devolucao.</param>
public sealed record DevolverSolicitacaoRegulacaoCommand(Guid SolicitacaoRegulacaoId, string Motivo) : ICommand;

/// <summary>Regras de validacao da devolucao de solicitacao de regulacao.</summary>
public sealed class DevolverSolicitacaoRegulacaoValidator : AbstractValidator<DevolverSolicitacaoRegulacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public DevolverSolicitacaoRegulacaoValidator()
    {
        RuleFor(comando => comando.SolicitacaoRegulacaoId).NotEmpty().WithMessage("Solicitacao e obrigatoria.");
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(1000).WithMessage("Motivo da devolucao e obrigatorio (max. 1000).");
    }
}

/// <summary>Handler da devolucao de solicitacao de regulacao.</summary>
public sealed class DevolverSolicitacaoRegulacaoHandler(
    ISolicitacaoRegulacaoRepository solicitacoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DevolverSolicitacaoRegulacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(DevolverSolicitacaoRegulacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var solicitacao = await solicitacoes
            .ObterPorIdAsync(new SolicitacaoRegulacaoId(request.SolicitacaoRegulacaoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Solicitacao nao encontrada.");

        solicitacao.Devolver(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
