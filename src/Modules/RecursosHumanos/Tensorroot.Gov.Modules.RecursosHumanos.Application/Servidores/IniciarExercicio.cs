using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>Inicia o efetivo exercicio de um servidor empossado.</summary>
/// <param name="ServidorId">Servidor a colocar em exercicio.</param>
/// <param name="DataExercicio">Data de inicio do exercicio.</param>
public sealed record IniciarExercicioCommand(Guid ServidorId, DateOnly DataExercicio) : ICommand;

/// <summary>Handler do inicio de exercicio.</summary>
public sealed class IniciarExercicioHandler(IServidorRepository servidores, IUnitOfWork unitOfWork)
    : ICommandHandler<IniciarExercicioCommand>
{
    /// <inheritdoc />
    public async Task Handle(IniciarExercicioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidor = await servidores.ObterPorIdAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        servidor.IniciarExercicio(request.DataExercicio);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
