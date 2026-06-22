using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Registra uma multa de trânsito (CTB) atribuída a um veículo.</summary>
/// <param name="VeiculoId">Veículo autuado.</param>
/// <param name="CodigoInfracaoCtb">Código de infração do CTB.</param>
/// <param name="Valor">Valor da multa.</param>
/// <param name="DataInfracao">Data da infração.</param>
/// <param name="MotoristaId">Condutor responsável, quando informado.</param>
public sealed record RegistrarMultaCommand(
    Guid VeiculoId,
    string CodigoInfracaoCtb,
    decimal Valor,
    DateOnly DataInfracao,
    Guid? MotoristaId) : ICommand;

/// <summary>Regras de validação do registro de multa.</summary>
public sealed class RegistrarMultaValidator : AbstractValidator<RegistrarMultaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarMultaValidator()
    {
        RuleFor(comando => comando.VeiculoId).NotEmpty();
        RuleFor(comando => comando.CodigoInfracaoCtb).NotEmpty();
        RuleFor(comando => comando.Valor).GreaterThan(0);
    }
}

/// <summary>Handler do registro de multa.</summary>
public sealed class RegistrarMultaHandler(IVeiculoRepository veiculos, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarMultaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarMultaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var veiculo = await veiculos.ObterPorIdAsync(new VeiculoId(request.VeiculoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Veículo não encontrado.");

        veiculo.RegistrarMulta(
            request.CodigoInfracaoCtb,
            ValorMonetario.De(request.Valor),
            request.DataInfracao,
            request.MotoristaId);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
