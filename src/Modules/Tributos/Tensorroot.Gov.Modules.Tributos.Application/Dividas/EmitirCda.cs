using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

namespace Tensorroot.Gov.Modules.Tributos.Application.Dividas;

/// <summary>Emite a Certidão de Dívida Ativa (CDA) de um título inscrito.</summary>
/// <param name="DividaAtivaId">Dívida ativa.</param>
/// <param name="NumeroCda">Número da CDA.</param>
public sealed record EmitirCdaCommand(Guid DividaAtivaId, string NumeroCda) : ICommand;

/// <summary>Regras de validação da emissão de CDA.</summary>
public sealed class EmitirCdaValidator : AbstractValidator<EmitirCdaCommand>
{
    /// <summary>Define as regras.</summary>
    public EmitirCdaValidator()
    {
        RuleFor(comando => comando.DividaAtivaId).NotEmpty();
        RuleFor(comando => comando.NumeroCda).NotEmpty().MaximumLength(40);
    }
}

/// <summary>Handler da emissão de CDA.</summary>
public sealed class EmitirCdaHandler(IDividaAtivaRepository dividas, IUnitOfWork unitOfWork)
    : ICommandHandler<EmitirCdaCommand>
{
    /// <inheritdoc />
    public async Task Handle(EmitirCdaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var divida = await dividas.ObterPorIdAsync(new DividaAtivaId(request.DividaAtivaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dívida ativa não encontrada.");

        divida.EmitirCda(request.NumeroCda);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
