using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Cargos;

/// <summary>Altera o vencimento-base de um cargo.</summary>
/// <param name="CargoId">Cargo.</param>
/// <param name="NovoVencimento">Novo valor do vencimento.</param>
public sealed record AlterarVencimentoCommand(Guid CargoId, decimal NovoVencimento) : ICommand;

/// <summary>Regras de validacao da alteracao de vencimento.</summary>
public sealed class AlterarVencimentoValidator : AbstractValidator<AlterarVencimentoCommand>
{
    /// <summary>Define as regras.</summary>
    public AlterarVencimentoValidator()
    {
        RuleFor(comando => comando.CargoId)
            .NotEmpty()
            .WithMessage("Cargo e obrigatorio.");

        RuleFor(comando => comando.NovoVencimento)
            .GreaterThan(0)
            .WithMessage("Vencimento deve ser maior que zero.");
    }
}

/// <summary>Handler da alteracao de vencimento.</summary>
public sealed class AlterarVencimentoHandler(ICargoRepository cargos, IUnitOfWork unitOfWork)
    : ICommandHandler<AlterarVencimentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AlterarVencimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cargo = await cargos.ObterPorIdAsync(new CargoId(request.CargoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cargo nao encontrado.");

        cargo.AlterarVencimento(Vencimento.De(request.NovoVencimento));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
