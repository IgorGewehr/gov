using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

namespace Tensorroot.Gov.Modules.Saude.Application.Regulacao;

/// <summary>Marca como executado (realizado) o procedimento de uma solicitacao autorizada.</summary>
/// <param name="SolicitacaoRegulacaoId">Solicitacao a executar.</param>
public sealed record ExecutarSolicitacaoRegulacaoCommand(Guid SolicitacaoRegulacaoId) : ICommand;

/// <summary>Regras de validacao da execucao de solicitacao de regulacao.</summary>
public sealed class ExecutarSolicitacaoRegulacaoValidator : AbstractValidator<ExecutarSolicitacaoRegulacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public ExecutarSolicitacaoRegulacaoValidator()
    {
        RuleFor(comando => comando.SolicitacaoRegulacaoId).NotEmpty().WithMessage("Solicitacao e obrigatoria.");
    }
}

/// <summary>Handler da execucao de solicitacao de regulacao.</summary>
public sealed class ExecutarSolicitacaoRegulacaoHandler(
    ISolicitacaoRegulacaoRepository solicitacoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ExecutarSolicitacaoRegulacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ExecutarSolicitacaoRegulacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var solicitacao = await solicitacoes
            .ObterPorIdAsync(new SolicitacaoRegulacaoId(request.SolicitacaoRegulacaoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Solicitacao nao encontrada.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        solicitacao.Executar(hoje);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
