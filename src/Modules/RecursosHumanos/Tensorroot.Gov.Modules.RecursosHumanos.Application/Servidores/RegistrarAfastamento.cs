using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>Registra um afastamento temporario de um servidor em atividade plena (dispara o S-2230).</summary>
/// <param name="ServidorId">Servidor a afastar.</param>
/// <param name="Inicio">Inicio do afastamento.</param>
/// <param name="Fim">Fim previsto do afastamento (nulo quando indeterminado).</param>
/// <param name="Motivo">Motivo do afastamento.</param>
public sealed record RegistrarAfastamentoCommand(Guid ServidorId, DateOnly Inicio, DateOnly? Fim, string Motivo) : ICommand;

/// <summary>Handler do registro de afastamento.</summary>
public sealed class RegistrarAfastamentoHandler(IServidorRepository servidores, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarAfastamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarAfastamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidor = await servidores.ObterPorIdAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        servidor.RegistrarAfastamento(request.Inicio, request.Fim, request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
