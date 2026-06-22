using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Cargos;

/// <summary>Libera (vacancia) uma vaga de um cargo.</summary>
/// <param name="CargoId">Cargo a vagar.</param>
public sealed record VagarCargoCommand(Guid CargoId) : ICommand;

/// <summary>Handler da vacancia de cargo.</summary>
public sealed class VagarCargoHandler(ICargoRepository cargos, IUnitOfWork unitOfWork)
    : ICommandHandler<VagarCargoCommand>
{
    /// <inheritdoc />
    public async Task Handle(VagarCargoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cargo = await cargos.ObterPorIdAsync(new CargoId(request.CargoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cargo nao encontrado.");

        cargo.Vagar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
