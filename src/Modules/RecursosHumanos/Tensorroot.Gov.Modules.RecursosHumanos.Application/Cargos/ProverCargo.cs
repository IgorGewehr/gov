using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Cargos;

/// <summary>Prove (preenche) uma vaga de um cargo.</summary>
/// <param name="CargoId">Cargo a prover.</param>
public sealed record ProverCargoCommand(Guid CargoId) : ICommand;

/// <summary>Handler do provimento de cargo.</summary>
public sealed class ProverCargoHandler(ICargoRepository cargos, IUnitOfWork unitOfWork)
    : ICommandHandler<ProverCargoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ProverCargoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cargo = await cargos.ObterPorIdAsync(new CargoId(request.CargoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cargo nao encontrado.");

        cargo.Prover();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
