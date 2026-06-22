using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Bens;

/// <summary>Tomba um bem incorporado, atribuindo número de tombo único por tenant.</summary>
/// <param name="BemPatrimonialId">Bem a tombar.</param>
/// <param name="NumeroTombamento">Número de tombamento.</param>
public sealed record TombarBemCommand(Guid BemPatrimonialId, string NumeroTombamento) : ICommand;

/// <summary>Regras de validação do tombamento.</summary>
public sealed class TombarBemValidator : AbstractValidator<TombarBemCommand>
{
    /// <summary>Define as regras.</summary>
    public TombarBemValidator()
    {
        RuleFor(comando => comando.BemPatrimonialId).NotEmpty();
        RuleFor(comando => comando.NumeroTombamento).NotEmpty().MaximumLength(40);
    }
}

/// <summary>Handler do tombamento de bem.</summary>
public sealed class TombarBemHandler(IBemPatrimonialRepository bens, IUnitOfWork unitOfWork)
    : ICommandHandler<TombarBemCommand>
{
    /// <inheritdoc />
    public async Task Handle(TombarBemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bem = await bens.ObterPorIdAsync(new BemPatrimonialId(request.BemPatrimonialId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Bem não encontrado.");

        bem.Tombar(request.NumeroTombamento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
