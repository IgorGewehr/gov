using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Estoque;

/// <summary>
/// Ajusta o valor realizável líquido do item e aplica o menor entre custo e VRL (I-2; NBC TSP 12).
/// </summary>
/// <param name="ItemEstoqueId">Item de estoque.</param>
/// <param name="ValorRealizavelLiquido">VRL informado (maior ou igual a zero).</param>
/// <param name="Data">Data do ajuste de mensuração.</param>
public sealed record AjustarValorRealizavelLiquidoCommand(
    Guid ItemEstoqueId,
    decimal ValorRealizavelLiquido,
    DateOnly Data) : ICommand;

/// <summary>Regras de validação do ajuste de VRL.</summary>
public sealed class AjustarValorRealizavelLiquidoValidator : AbstractValidator<AjustarValorRealizavelLiquidoCommand>
{
    /// <summary>Define as regras.</summary>
    public AjustarValorRealizavelLiquidoValidator()
    {
        RuleFor(comando => comando.ItemEstoqueId).NotEmpty();
        RuleFor(comando => comando.ValorRealizavelLiquido).GreaterThanOrEqualTo(0m);
    }
}

/// <summary>Handler do ajuste de valor realizável líquido.</summary>
public sealed class AjustarValorRealizavelLiquidoHandler(
    IItemEstoqueRepository itens,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AjustarValorRealizavelLiquidoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AjustarValorRealizavelLiquidoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = await itens.ObterPorIdAsync(new ItemEstoqueId(request.ItemEstoqueId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Item de estoque não encontrado.");

        item.AjustarValorRealizavelLiquido(ValorMonetario.De(request.ValorRealizavelLiquido), request.Data);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
