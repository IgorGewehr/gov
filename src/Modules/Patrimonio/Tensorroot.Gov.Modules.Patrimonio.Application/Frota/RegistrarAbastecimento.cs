using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Registra um abastecimento de um veículo da frota, sob cota e medições controladas.</summary>
/// <param name="VeiculoId">Veículo abastecido.</param>
/// <param name="Data">Data do abastecimento.</param>
/// <param name="Litros">Litros abastecidos.</param>
/// <param name="Valor">Valor do abastecimento.</param>
/// <param name="Odometro">Leitura do odômetro no ato.</param>
/// <param name="Horimetro">Leitura do horímetro no ato.</param>
/// <param name="MotoristaId">Motorista, quando informado.</param>
public sealed record RegistrarAbastecimentoCommand(
    Guid VeiculoId,
    DateOnly Data,
    decimal Litros,
    decimal Valor,
    int Odometro,
    decimal Horimetro,
    Guid? MotoristaId) : ICommand;

/// <summary>Regras de validação do registro de abastecimento.</summary>
public sealed class RegistrarAbastecimentoValidator : AbstractValidator<RegistrarAbastecimentoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarAbastecimentoValidator()
    {
        RuleFor(comando => comando.VeiculoId).NotEmpty();
        RuleFor(comando => comando.Litros).GreaterThan(0);
        RuleFor(comando => comando.Valor).GreaterThan(0);
        RuleFor(comando => comando.Odometro).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Handler do registro de abastecimento.</summary>
public sealed class RegistrarAbastecimentoHandler(IVeiculoRepository veiculos, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarAbastecimentoCommand>
{
    /// <summary>
    /// Cota vigente quando ainda não há integração ativa com o gestor de combustível:
    /// não impõe limite (a validação de cota I-6 é exercida pela ACL do gestor externo).
    /// </summary>
    private const decimal CotaSemLimiteConfigurado = decimal.MaxValue;

    /// <inheritdoc />
    public async Task Handle(RegistrarAbastecimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var veiculo = await veiculos.ObterPorIdAsync(new VeiculoId(request.VeiculoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Veículo não encontrado.");

        veiculo.RegistrarAbastecimento(
            request.Data,
            request.Litros,
            ValorMonetario.De(request.Valor),
            request.Odometro,
            request.Horimetro,
            CotaSemLimiteConfigurado,
            request.MotoristaId);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
