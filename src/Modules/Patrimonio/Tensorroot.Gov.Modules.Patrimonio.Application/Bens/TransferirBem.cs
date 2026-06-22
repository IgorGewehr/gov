using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Bens;

/// <summary>Transfere um bem para nova localização/responsável (movimentação interna).</summary>
/// <param name="BemPatrimonialId">Bem a transferir.</param>
/// <param name="LocalizacaoDestino">Localização de destino.</param>
/// <param name="ResponsavelDestinoId">Responsável de destino.</param>
public sealed record TransferirBemCommand(
    Guid BemPatrimonialId,
    string LocalizacaoDestino,
    Guid ResponsavelDestinoId) : ICommand;

/// <summary>Handler da transferência de bem.</summary>
public sealed class TransferirBemHandler(
    IBemPatrimonialRepository bens,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<TransferirBemCommand>
{
    /// <inheritdoc />
    public async Task Handle(TransferirBemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bem = await bens.ObterPorIdAsync(new BemPatrimonialId(request.BemPatrimonialId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Bem não encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        bem.Transferir(request.LocalizacaoDestino, request.ResponsavelDestinoId, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
