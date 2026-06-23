using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Inventarios;

/// <summary>Congela o snapshot contábil do acervo no inventário (avança para EmContagem).</summary>
/// <param name="InventarioId">Inventário a carregar.</param>
public sealed record CarregarSnapshotCommand(Guid InventarioId) : ICommand;

/// <summary>Regras de validação do carregamento de snapshot.</summary>
public sealed class CarregarSnapshotValidator : AbstractValidator<CarregarSnapshotCommand>
{
    /// <summary>Define as regras.</summary>
    public CarregarSnapshotValidator() => RuleFor(comando => comando.InventarioId).NotEmpty();
}

/// <summary>Handler do carregamento de snapshot contábil.</summary>
public sealed class CarregarSnapshotHandler(
    IInventarioRepository inventarios,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CarregarSnapshotCommand>
{
    /// <inheritdoc />
    public async Task Handle(CarregarSnapshotCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inventario = await inventarios.ObterPorIdAsync(new InventarioId(request.InventarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inventário não encontrado.");

        var snapshot = await inventarios.CarregarSnapshotAcervoAsync(inventario.Setor, cancellationToken).ConfigureAwait(false);
        inventario.CarregarSnapshotContabil(snapshot);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
