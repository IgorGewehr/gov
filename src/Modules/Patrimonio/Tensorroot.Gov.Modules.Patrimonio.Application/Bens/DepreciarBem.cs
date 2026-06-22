using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Contracts;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Bens;

/// <summary>Reconhece a depreciação linear de um bem para a competência informada.</summary>
/// <param name="BemPatrimonialId">Bem a depreciar.</param>
/// <param name="AnoCompetencia">Ano da competência.</param>
/// <param name="MesCompetencia">Mês da competência (1 a 12).</param>
public sealed record DepreciarBemCommand(Guid BemPatrimonialId, int AnoCompetencia, int MesCompetencia) : ICommand;

/// <summary>Handler da depreciação de bem.</summary>
public sealed class DepreciarBemHandler(
    IBemPatrimonialRepository bens,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    TimeProvider timeProvider)
    : ICommandHandler<DepreciarBemCommand>
{
    /// <inheritdoc />
    public async Task Handle(DepreciarBemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bem = await bens.ObterPorIdAsync(new BemPatrimonialId(request.BemPatrimonialId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Bem não encontrado.");

        var competencia = new DateOnly(request.AnoCompetencia, request.MesCompetencia, 1);
        var valorDepreciado = bem.Depreciar(competencia);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new BemDepreciadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            bem.TenantId,
            bem.Id.Value,
            valorDepreciado,
            competencia);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
