using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;

/// <summary>Registra o parecer de uma Comissao sobre uma proposicao em tramitacao.</summary>
/// <param name="ProposicaoId">Proposicao alvo.</param>
/// <param name="Comissao">Comissao emitente (ex.: Ccj, FinancasOrcamento).</param>
/// <param name="Favoravel">Sentido do parecer (favoravel/contrario).</param>
public sealed record RegistrarParecerCommand(Guid ProposicaoId, string Comissao, bool Favoravel) : ICommand;

/// <summary>Regras de validacao do registro de parecer.</summary>
public sealed class RegistrarParecerValidator : AbstractValidator<RegistrarParecerCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarParecerValidator()
    {
        RuleFor(comando => comando.ProposicaoId).NotEmpty();
        RuleFor(comando => comando.Comissao).NotEmpty().MaximumLength(120);
    }
}

/// <summary>Handler do registro de parecer.</summary>
public sealed class RegistrarParecerHandler(
    IProposicaoRepository proposicoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarParecerCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarParecerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var proposicao = await proposicoes.ObterPorIdAsync(new ProposicaoId(request.ProposicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Proposicao nao encontrada.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        proposicao.RegistrarParecer(request.Comissao, request.Favoravel, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
