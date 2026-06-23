using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Inventarios;

/// <summary>Cancela o inventário (terminal) sem efeito patrimonial.</summary>
/// <param name="InventarioId">Inventário a cancelar.</param>
/// <param name="Motivo">Motivo do cancelamento.</param>
public sealed record CancelarInventarioCommand(Guid InventarioId, string Motivo) : ICommand;

/// <summary>Regras de validação do cancelamento de inventário.</summary>
public sealed class CancelarInventarioValidator : AbstractValidator<CancelarInventarioCommand>
{
    /// <summary>Define as regras.</summary>
    public CancelarInventarioValidator()
    {
        RuleFor(comando => comando.InventarioId).NotEmpty();
        RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(500);
    }
}

/// <summary>Handler do cancelamento de inventário.</summary>
public sealed class CancelarInventarioHandler(
    IInventarioRepository inventarios,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarInventarioCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarInventarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inventario = await inventarios.ObterPorIdAsync(new InventarioId(request.InventarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Inventário não encontrado.");

        inventario.Cancelar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
