using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Portarias;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Portarias;

/// <summary>Revoga (torna sem efeito) uma portaria; preserva a numeracao consumida no exercicio.</summary>
/// <param name="PortariaId">Portaria a revogar.</param>
/// <param name="Motivo">Fundamento da revogacao.</param>
public sealed record RevogarPortariaCommand(Guid PortariaId, string Motivo) : ICommand;

/// <summary>Regras de validacao da revogacao de portaria.</summary>
public sealed class RevogarPortariaValidator : AbstractValidator<RevogarPortariaCommand>
{
    /// <summary>Define as regras.</summary>
    public RevogarPortariaValidator()
    {
        RuleFor(comando => comando.PortariaId).NotEmpty().WithMessage("Portaria e obrigatoria.");
        RuleFor(comando => comando.Motivo).NotEmpty().WithMessage("Motivo da revogacao e obrigatorio.");
    }
}

/// <summary>Handler da revogacao de portaria.</summary>
public sealed class RevogarPortariaHandler(IPortariaRepository portarias, IUnitOfWork unitOfWork)
    : ICommandHandler<RevogarPortariaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RevogarPortariaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var portaria = await portarias.ObterPorIdAsync(new PortariaId(request.PortariaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Portaria nao encontrada.");

        portaria.Revogar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
