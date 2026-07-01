using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;

/// <summary>Inclui uma proposicao na Ordem do Dia de uma sessao nao terminal.</summary>
/// <param name="SessaoId">Sessao alvo.</param>
/// <param name="ProposicaoId">Proposicao a pautar.</param>
public sealed record IncluirNaOrdemDoDiaCommand(Guid SessaoId, Guid ProposicaoId) : ICommand;

/// <summary>Regras de validacao da inclusao em Ordem do Dia.</summary>
public sealed class IncluirNaOrdemDoDiaValidator : AbstractValidator<IncluirNaOrdemDoDiaCommand>
{
    /// <summary>Define as regras.</summary>
    public IncluirNaOrdemDoDiaValidator()
    {
        RuleFor(comando => comando.SessaoId).NotEmpty();
        RuleFor(comando => comando.ProposicaoId).NotEmpty();
    }
}

/// <summary>Handler da inclusao em Ordem do Dia.</summary>
public sealed class IncluirNaOrdemDoDiaHandler(
    ISessaoRepository sessoes,
    IProposicaoRepository proposicoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<IncluirNaOrdemDoDiaCommand>
{
    /// <inheritdoc />
    public async Task Handle(IncluirNaOrdemDoDiaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessao = await sessoes.ObterPorIdAsync(new SessaoId(request.SessaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sessao nao encontrada.");

        // Integridade referencial + isolamento: a proposição deve EXISTIR no tenant (o repositório
        // aplica o Global Query Filter) antes de ser pautada — evita Ordem do Dia com referência órfã.
        _ = await proposicoes.ObterPorIdAsync(new ProposicaoId(request.ProposicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Proposicao nao encontrada.");

        sessao.IncluirNaOrdemDoDia(new ProposicaoId(request.ProposicaoId));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
