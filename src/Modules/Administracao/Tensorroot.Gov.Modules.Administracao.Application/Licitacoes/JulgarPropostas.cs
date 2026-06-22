using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;

/// <summary>Julga as propostas: indica a vencedora e passa o certame a <c>EmJulgamento</c>.</summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
/// <param name="PropostaVencedoraId">Proposta indicada como vencedora.</param>
public sealed record JulgarPropostasCommand(Guid LicitacaoId, Guid PropostaVencedoraId) : ICommand;

/// <summary>Regras de validacao do julgamento de propostas.</summary>
public sealed class JulgarPropostasValidator : AbstractValidator<JulgarPropostasCommand>
{
    /// <summary>Define as regras.</summary>
    public JulgarPropostasValidator()
    {
        RuleFor(comando => comando.LicitacaoId).NotEmpty().WithMessage("Licitacao e obrigatoria.");
        RuleFor(comando => comando.PropostaVencedoraId).NotEmpty().WithMessage("Proposta vencedora e obrigatoria.");
    }
}

/// <summary>Handler do julgamento de propostas.</summary>
public sealed class JulgarPropostasHandler(
    ILicitacaoRepository licitacoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<JulgarPropostasCommand>
{
    /// <inheritdoc />
    public async Task Handle(JulgarPropostasCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var licitacao = await licitacoes.ObterPorIdAsync(new LicitacaoId(request.LicitacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Licitacao nao encontrada.");

        licitacao.JulgarPropostas(request.PropostaVencedoraId);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
