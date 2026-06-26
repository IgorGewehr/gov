using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;

namespace Tensorroot.Gov.Modules.Administracao.Application.Dispensas;

/// <summary>Abre a etapa de envio de lances da dispensa (IN SEGES/ME 67/2021, art. 9).</summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
public sealed record AbrirDisputaDispensaCommand(Guid DispensaId) : ICommand;

/// <summary>Regras de validacao da abertura da disputa.</summary>
public sealed class AbrirDisputaDispensaValidator : AbstractValidator<AbrirDisputaDispensaCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirDisputaDispensaValidator()
        => RuleFor(comando => comando.DispensaId).NotEmpty().WithMessage("Dispensa e obrigatoria.");
}

/// <summary>Handler da abertura da etapa de lances.</summary>
public sealed class AbrirDisputaDispensaHandler(
    IDispensaRepository dispensas,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AbrirDisputaDispensaCommand>
{
    /// <inheritdoc />
    public async Task Handle(AbrirDisputaDispensaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispensa = await dispensas.ObterPorIdAsync(new DispensaEletronicaId(request.DispensaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dispensa nao encontrada.");

        // O instante de abertura vem do relogio externo (TimeProvider); o agregado nao le relogio.
        dispensa.AbrirDisputa(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
