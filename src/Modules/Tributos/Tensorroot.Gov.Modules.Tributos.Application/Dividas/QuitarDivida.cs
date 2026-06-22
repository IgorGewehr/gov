using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Contracts;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

namespace Tensorroot.Gov.Modules.Tributos.Application.Dividas;

/// <summary>Quita uma Dívida Ativa e publica a receita arrecadada para os demais módulos.</summary>
/// <param name="DividaAtivaId">Dívida ativa a quitar.</param>
public sealed record QuitarDividaCommand(Guid DividaAtivaId) : ICommand;

/// <summary>Handler da quitação de Dívida Ativa (publica Integration Event de receita).</summary>
public sealed class QuitarDividaHandler(
    IDividaAtivaRepository dividas,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<QuitarDividaCommand>
{
    /// <inheritdoc />
    public async Task Handle(QuitarDividaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var divida = await dividas.ObterPorIdAsync(new DividaAtivaId(request.DividaAtivaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dívida ativa não encontrada.");

        divida.Quitar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var agoraUtc = timeProvider.GetUtcNow().UtcDateTime;
        var evento = new ReceitaArrecadadaIntegrationEvent(
            Guid.NewGuid(),
            agoraUtc,
            tenant.TenantId,
            request.DividaAtivaId,
            divida.ValorInscrito.Valor,
            DateOnly.FromDateTime(agoraUtc));

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
