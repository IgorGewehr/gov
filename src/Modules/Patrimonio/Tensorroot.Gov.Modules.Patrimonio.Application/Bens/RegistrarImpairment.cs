using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Bens;

/// <summary>Reconhece perda por impairment quando o valor recuperável é inferior ao contábil.</summary>
/// <param name="BemPatrimonialId">Bem testado.</param>
/// <param name="ValorRecuperavel">Valor recuperável apurado.</param>
/// <param name="LaudoUri">Referência (URI) do laudo/teste (obrigatório).</param>
/// <param name="DataTeste">Data do teste de recuperabilidade.</param>
public sealed record RegistrarImpairmentCommand(
    Guid BemPatrimonialId,
    decimal ValorRecuperavel,
    string LaudoUri,
    DateOnly DataTeste) : ICommand;

/// <summary>Handler do registro de impairment.</summary>
public sealed class RegistrarImpairmentHandler(IBemPatrimonialRepository bens, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarImpairmentCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarImpairmentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bem = await bens.ObterPorIdAsync(new BemPatrimonialId(request.BemPatrimonialId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Bem não encontrado.");

        bem.RegistrarImpairment(request.ValorRecuperavel, request.LaudoUri, request.DataTeste);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
