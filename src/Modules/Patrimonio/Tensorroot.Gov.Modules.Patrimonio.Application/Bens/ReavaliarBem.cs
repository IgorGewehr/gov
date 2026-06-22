using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Contracts;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Bens;

/// <summary>Reavalia um bem ao valor justo informado (somente bem ativo no acervo).</summary>
/// <param name="BemPatrimonialId">Bem a reavaliar.</param>
/// <param name="NovoValorJusto">Novo valor justo.</param>
/// <param name="LaudoUri">Referência (URI) do laudo (obrigatório).</param>
/// <param name="DataReavaliacao">Data da reavaliação.</param>
public sealed record ReavaliarBemCommand(
    Guid BemPatrimonialId,
    decimal NovoValorJusto,
    string LaudoUri,
    DateOnly DataReavaliacao) : ICommand;

/// <summary>Handler da reavaliação de bem.</summary>
public sealed class ReavaliarBemHandler(
    IBemPatrimonialRepository bens,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    TimeProvider timeProvider)
    : ICommandHandler<ReavaliarBemCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReavaliarBemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bem = await bens.ObterPorIdAsync(new BemPatrimonialId(request.BemPatrimonialId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Bem não encontrado.");

        bem.Reavaliar(request.NovoValorJusto, request.LaudoUri, request.DataReavaliacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new BemReavaliadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            bem.TenantId,
            bem.Id.Value,
            bem.ValorContabil.Valor);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);
    }
}
