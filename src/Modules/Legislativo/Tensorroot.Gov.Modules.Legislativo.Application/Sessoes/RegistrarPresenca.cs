using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;

/// <summary>Registra a presenca de um vereador (idempotente por vereador) numa sessao nao terminal.</summary>
/// <param name="SessaoId">Sessao alvo.</param>
/// <param name="VereadorId">Vereador presente.</param>
public sealed record RegistrarPresencaCommand(Guid SessaoId, Guid VereadorId) : ICommand;

/// <summary>Regras de validacao do registro de presenca.</summary>
public sealed class RegistrarPresencaValidator : AbstractValidator<RegistrarPresencaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarPresencaValidator()
    {
        RuleFor(comando => comando.SessaoId).NotEmpty();
        RuleFor(comando => comando.VereadorId).NotEmpty();
    }
}

/// <summary>Handler do registro de presenca.</summary>
public sealed class RegistrarPresencaHandler(
    ISessaoRepository sessoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RegistrarPresencaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarPresencaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessao = await sessoes.ObterPorIdAsync(new SessaoId(request.SessaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sessao nao encontrada.");

        sessao.RegistrarPresenca(new VereadorId(request.VereadorId), timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
