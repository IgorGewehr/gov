using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Bens;

/// <summary>Aliena um bem (exige avaliação prévia e, em regra, leilão — Lei 14.133 art. 31/76; I-9).</summary>
/// <param name="BemPatrimonialId">Bem a alienar.</param>
/// <param name="AvaliacaoPreviaId">Identificador da avaliação prévia (obrigatório).</param>
/// <param name="PorLeilao">Indica se a alienação foi por leilão.</param>
/// <param name="ValorAlienacao">Valor da alienação.</param>
public sealed record AlienarBemCommand(
    Guid BemPatrimonialId,
    Guid AvaliacaoPreviaId,
    bool PorLeilao,
    decimal ValorAlienacao) : ICommand;

/// <summary>Regras de validação da alienação de bem.</summary>
public sealed class AlienarBemValidator : AbstractValidator<AlienarBemCommand>
{
    /// <summary>Define as regras.</summary>
    public AlienarBemValidator()
    {
        RuleFor(comando => comando.BemPatrimonialId).NotEmpty();
        RuleFor(comando => comando.AvaliacaoPreviaId).NotEmpty();
    }
}

/// <summary>Handler da alienação de bem (o evento de domínio <c>BemBaixado</c> alimenta o Outbox para Finanças — I-8).</summary>
public sealed class AlienarBemHandler(IBemPatrimonialRepository bens, IUnitOfWork unitOfWork)
    : ICommandHandler<AlienarBemCommand>
{
    /// <inheritdoc />
    public async Task Handle(AlienarBemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bem = await bens.ObterPorIdAsync(new BemPatrimonialId(request.BemPatrimonialId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Bem não encontrado.");

        bem.Alienar(request.AvaliacaoPreviaId, request.PorLeilao, request.ValorAlienacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
