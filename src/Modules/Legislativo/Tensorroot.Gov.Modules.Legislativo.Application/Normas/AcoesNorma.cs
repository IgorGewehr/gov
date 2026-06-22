using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Normas;

/// <summary>Revoga uma norma em vigor/alterada — N-4.</summary>
/// <param name="NormaId">Norma a revogar.</param>
/// <param name="DataRevogacao">Data da revogacao.</param>
/// <param name="NormaRevogadoraId">Norma revogadora (opcional).</param>
public sealed record RevogarNormaCommand(Guid NormaId, DateOnly DataRevogacao, Guid? NormaRevogadoraId) : ICommand;

/// <summary>Regras de validacao da revogacao de norma.</summary>
public sealed class RevogarNormaValidator : AbstractValidator<RevogarNormaCommand>
{
    /// <summary>Define as regras.</summary>
    public RevogarNormaValidator() => RuleFor(comando => comando.NormaId).NotEmpty();
}

/// <summary>Handler da revogacao de norma.</summary>
public sealed class RevogarNormaHandler(INormaRepository normas, IUnitOfWork unitOfWork)
    : ICommandHandler<RevogarNormaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RevogarNormaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var norma = await normas.ObterPorIdAsync(new NormaId(request.NormaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Norma nao encontrada.");

        norma.Revogar(
            request.DataRevogacao,
            request.NormaRevogadoraId is { } id ? new NormaId(id) : null);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Registra uma alteracao de norma por norma posterior — N-5.</summary>
/// <param name="NormaId">Norma alterada.</param>
/// <param name="DataReferencia">Data da alteracao.</param>
/// <param name="NormaAlteradoraId">Norma que altera.</param>
public sealed record RegistrarAlteracaoNormaCommand(Guid NormaId, DateOnly DataReferencia, Guid NormaAlteradoraId) : ICommand;

/// <summary>Regras de validacao do registro de alteracao.</summary>
public sealed class RegistrarAlteracaoNormaValidator : AbstractValidator<RegistrarAlteracaoNormaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarAlteracaoNormaValidator()
    {
        RuleFor(comando => comando.NormaId).NotEmpty();
        RuleFor(comando => comando.NormaAlteradoraId).NotEmpty();
    }
}

/// <summary>Handler do registro de alteracao.</summary>
public sealed class RegistrarAlteracaoNormaHandler(INormaRepository normas, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarAlteracaoNormaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarAlteracaoNormaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var norma = await normas.ObterPorIdAsync(new NormaId(request.NormaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Norma nao encontrada.");

        norma.RegistrarAlteracao(request.DataReferencia, new NormaId(request.NormaAlteradoraId));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Vincula a proposicao de origem a uma norma (idempotente) — N-6.</summary>
/// <param name="NormaId">Norma.</param>
/// <param name="ProposicaoId">Proposicao de origem.</param>
public sealed record VincularProposicaoOrigemCommand(Guid NormaId, Guid ProposicaoId) : ICommand;

/// <summary>Regras de validacao do vinculo de proposicao de origem.</summary>
public sealed class VincularProposicaoOrigemValidator : AbstractValidator<VincularProposicaoOrigemCommand>
{
    /// <summary>Define as regras.</summary>
    public VincularProposicaoOrigemValidator()
    {
        RuleFor(comando => comando.NormaId).NotEmpty();
        RuleFor(comando => comando.ProposicaoId).NotEmpty();
    }
}

/// <summary>Handler do vinculo de proposicao de origem.</summary>
public sealed class VincularProposicaoOrigemHandler(INormaRepository normas, IUnitOfWork unitOfWork)
    : ICommandHandler<VincularProposicaoOrigemCommand>
{
    /// <inheritdoc />
    public async Task Handle(VincularProposicaoOrigemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var norma = await normas.ObterPorIdAsync(new NormaId(request.NormaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Norma nao encontrada.");

        norma.VincularProposicaoOrigem(new ProposicaoId(request.ProposicaoId));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
