using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;

namespace Tensorroot.Gov.Modules.Administracao.Application.Dispensas;

/// <summary>
/// Encerra a etapa de lances e julga: classifica e indica a cotacao vencedora pelo criterio, passando a
/// dispensa a <c>EmJulgamento</c> (IN SEGES/ME 67/2021).
/// </summary>
/// <param name="DispensaId">Identificador da dispensa.</param>
public sealed record EncerrarDisputaDispensaCommand(Guid DispensaId) : ICommand;

/// <summary>Regras de validacao do encerramento/julgamento.</summary>
public sealed class EncerrarDisputaDispensaValidator : AbstractValidator<EncerrarDisputaDispensaCommand>
{
    /// <summary>Define as regras.</summary>
    public EncerrarDisputaDispensaValidator()
        => RuleFor(comando => comando.DispensaId).NotEmpty().WithMessage("Dispensa e obrigatoria.");
}

/// <summary>Handler do encerramento da disputa e julgamento.</summary>
public sealed class EncerrarDisputaDispensaHandler(
    IDispensaRepository dispensas,
    IUnitOfWork unitOfWork)
    : ICommandHandler<EncerrarDisputaDispensaCommand>
{
    /// <inheritdoc />
    public async Task Handle(EncerrarDisputaDispensaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dispensa = await dispensas.ObterPorIdAsync(new DispensaEletronicaId(request.DispensaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dispensa nao encontrada.");

        dispensa.EncerrarDisputaEJulgar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
