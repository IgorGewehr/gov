using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Estoque;

/// <summary>
/// Registra a entrada de itens no almoxarifado: cria/atualiza lote (PEPS), recalcula o custo
/// médio (médio) e incrementa o saldo — sem reconhecer despesa (I-4/I-5).
/// </summary>
/// <param name="ItemEstoqueId">Item de estoque.</param>
/// <param name="Quantidade">Quantidade que ingressa (estritamente positiva).</param>
/// <param name="CustoUnitario">Custo unitário de entrada.</param>
/// <param name="DataEntrada">Data da entrada.</param>
/// <param name="Validade">Validade opcional do lote.</param>
/// <param name="Documento">Documento de respaldo (NF/recebimento).</param>
public sealed record RegistrarEntradaCommand(
    Guid ItemEstoqueId,
    decimal Quantidade,
    decimal CustoUnitario,
    DateOnly DataEntrada,
    DateOnly? Validade,
    string Documento) : ICommand;

/// <summary>Regras de validação do registro de entrada.</summary>
public sealed class RegistrarEntradaValidator : AbstractValidator<RegistrarEntradaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarEntradaValidator()
    {
        RuleFor(comando => comando.ItemEstoqueId).NotEmpty();
        RuleFor(comando => comando.Quantidade).GreaterThan(0m);
        RuleFor(comando => comando.CustoUnitario).GreaterThanOrEqualTo(0m);
    }
}

/// <summary>Handler do registro de entrada de estoque.</summary>
public sealed class RegistrarEntradaHandler(
    IItemEstoqueRepository itens,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarEntradaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarEntradaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = await itens.ObterPorIdAsync(new ItemEstoqueId(request.ItemEstoqueId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Item de estoque não encontrado.");

        item.RegistrarEntrada(
            request.Quantidade,
            ValorMonetario.De(request.CustoUnitario),
            request.DataEntrada,
            request.Validade,
            request.Documento);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
