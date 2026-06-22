using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Estoque;

/// <summary>Reclassifica o item na Curva ABC (A/B/C).</summary>
/// <param name="ItemEstoqueId">Item de estoque.</param>
/// <param name="ClassificacaoAbc">Nova classe ABC.</param>
public sealed record ReclassificarAbcCommand(
    Guid ItemEstoqueId,
    int ClassificacaoAbc) : ICommand;

/// <summary>Regras de validação da reclassificação ABC.</summary>
public sealed class ReclassificarAbcValidator : AbstractValidator<ReclassificarAbcCommand>
{
    /// <summary>Define as regras.</summary>
    public ReclassificarAbcValidator()
    {
        RuleFor(comando => comando.ItemEstoqueId).NotEmpty();
        RuleFor(comando => (CurvaABC)comando.ClassificacaoAbc).IsInEnum();
    }
}

/// <summary>Handler da reclassificação na Curva ABC.</summary>
public sealed class ReclassificarAbcHandler(
    IItemEstoqueRepository itens,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReclassificarAbcCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReclassificarAbcCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = await itens.ObterPorIdAsync(new ItemEstoqueId(request.ItemEstoqueId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Item de estoque não encontrado.");

        item.ReclassificarAbc((CurvaABC)request.ClassificacaoAbc);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
