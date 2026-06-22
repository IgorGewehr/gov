using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Estoque;

/// <summary>Inativa um item de estoque — só permitido com saldo zero.</summary>
/// <param name="ItemEstoqueId">Item de estoque.</param>
public sealed record InativarItemCommand(Guid ItemEstoqueId) : ICommand;

/// <summary>Regras de validação da inativação de item.</summary>
public sealed class InativarItemValidator : AbstractValidator<InativarItemCommand>
{
    /// <summary>Define as regras.</summary>
    public InativarItemValidator()
    {
        RuleFor(comando => comando.ItemEstoqueId).NotEmpty();
    }
}

/// <summary>Handler da inativação de item de estoque.</summary>
public sealed class InativarItemHandler(
    IItemEstoqueRepository itens,
    IUnitOfWork unitOfWork)
    : ICommandHandler<InativarItemCommand>
{
    /// <inheritdoc />
    public async Task Handle(InativarItemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = await itens.ObterPorIdAsync(new ItemEstoqueId(request.ItemEstoqueId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Item de estoque não encontrado.");

        item.Inativar();

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
