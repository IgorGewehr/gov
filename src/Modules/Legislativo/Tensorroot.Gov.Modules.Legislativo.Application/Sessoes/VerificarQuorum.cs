using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;

/// <summary>Apura se o quorum de instalacao de uma sessao foi atingido (emite QuorumVerificado).</summary>
/// <param name="SessaoId">Sessao alvo.</param>
public sealed record VerificarQuorumCommand(Guid SessaoId) : ICommand<bool>;

/// <summary>Regras de validacao da verificacao de quorum.</summary>
public sealed class VerificarQuorumValidator : AbstractValidator<VerificarQuorumCommand>
{
    /// <summary>Define as regras.</summary>
    public VerificarQuorumValidator()
    {
        RuleFor(comando => comando.SessaoId).NotEmpty();
    }
}

/// <summary>Handler da verificacao de quorum.</summary>
public sealed class VerificarQuorumHandler(
    ISessaoRepository sessoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<VerificarQuorumCommand, bool>
{
    /// <inheritdoc />
    public async Task<bool> Handle(VerificarQuorumCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessao = await sessoes.ObterPorIdAsync(new SessaoId(request.SessaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sessao nao encontrada.");

        var atingido = sessao.VerificarQuorum();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return atingido;
    }
}
