using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Bens;

/// <summary>Cede um bem em cessão/comodato a terceiro, mantendo-o no acervo (sem baixa contábil).</summary>
/// <param name="BemPatrimonialId">Bem a ceder.</param>
/// <param name="TerceiroId">Terceiro beneficiário.</param>
/// <param name="Gratuito">Indica se a cessão é a título gratuito (comodato).</param>
/// <param name="DataInicio">Início da cessão.</param>
/// <param name="DataFim">Fim da cessão (opcional).</param>
public sealed record CederBemCommand(
    Guid BemPatrimonialId,
    Guid TerceiroId,
    bool Gratuito,
    DateOnly DataInicio,
    DateOnly? DataFim) : ICommand;

/// <summary>Handler da cessão/comodato de bem.</summary>
public sealed class CederBemHandler(IBemPatrimonialRepository bens, IUnitOfWork unitOfWork)
    : ICommandHandler<CederBemCommand>
{
    /// <inheritdoc />
    public async Task Handle(CederBemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bem = await bens.ObterPorIdAsync(new BemPatrimonialId(request.BemPatrimonialId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Bem não encontrado.");

        bem.Ceder();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
