using System;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>Concede a estabilidade a um servidor efetivo apos 3 anos de efetivo exercicio (CF art. 41).</summary>
/// <param name="ServidorId">Servidor a estabilizar.</param>
public sealed record ConcederEstabilidadeCommand(Guid ServidorId) : ICommand;

/// <summary>Handler da concessao de estabilidade.</summary>
public sealed class ConcederEstabilidadeHandler(
    IServidorRepository servidores,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ConcederEstabilidadeCommand>
{
    /// <inheritdoc />
    public async Task Handle(ConcederEstabilidadeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidor = await servidores.ObterPorIdAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        // TODO(fuso): trocar por IDataHojeTenant.Hoje() (prazo/data de dominio no fuso do tenant; ver W9 fix de fuso).
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        servidor.ConcederEstabilidade(hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
