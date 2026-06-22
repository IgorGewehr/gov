using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Cargos;

/// <summary>Extingue um cargo por lei.</summary>
/// <param name="CargoId">Cargo a extinguir.</param>
/// <param name="LeiExtincao">Lei que extingue o cargo.</param>
public sealed record ExtinguirCargoCommand(Guid CargoId, string LeiExtincao) : ICommand;

/// <summary>Regras de validacao da extincao de cargo.</summary>
public sealed class ExtinguirCargoValidator : AbstractValidator<ExtinguirCargoCommand>
{
    /// <summary>Define as regras.</summary>
    public ExtinguirCargoValidator()
    {
        RuleFor(comando => comando.CargoId)
            .NotEmpty()
            .WithMessage("Cargo e obrigatorio.");

        RuleFor(comando => comando.LeiExtincao)
            .NotEmpty()
            .MaximumLength(80)
            .WithMessage("Lei de extincao e obrigatoria.");
    }
}

/// <summary>Handler da extincao de cargo.</summary>
public sealed class ExtinguirCargoHandler(ICargoRepository cargos, IUnitOfWork unitOfWork)
    : ICommandHandler<ExtinguirCargoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ExtinguirCargoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cargo = await cargos.ObterPorIdAsync(new CargoId(request.CargoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cargo nao encontrado.");

        cargo.Extinguir(request.LeiExtincao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
