using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Incorpora um novo veículo à frota (bem patrimonial), retornando seu identificador.</summary>
/// <param name="Descricao">Descrição do veículo.</param>
/// <param name="ValorInicial">Valor de incorporação.</param>
/// <param name="ValorResidual">Valor residual.</param>
/// <param name="VidaUtilMeses">Vida útil estimada, em meses.</param>
/// <param name="DataIncorporacao">Data de incorporação.</param>
/// <param name="Origem">Origem do ingresso.</param>
/// <param name="Placa">Placa do veículo (CTB).</param>
/// <param name="Renavam">RENAVAM do veículo (CTB).</param>
/// <param name="OdometroInicial">Quilometragem inicial.</param>
/// <param name="HorimetroInicial">Horas de uso iniciais.</param>
public sealed record IncorporarVeiculoCommand(
    string Descricao,
    decimal ValorInicial,
    decimal ValorResidual,
    int VidaUtilMeses,
    DateOnly DataIncorporacao,
    string Origem,
    string Placa,
    string Renavam,
    int OdometroInicial,
    decimal HorimetroInicial) : ICommand<Guid>;

/// <summary>Regras de validação da incorporação de veículo.</summary>
public sealed class IncorporarVeiculoValidator : AbstractValidator<IncorporarVeiculoCommand>
{
    /// <summary>Define as regras.</summary>
    public IncorporarVeiculoValidator()
    {
        RuleFor(comando => comando.Descricao).NotEmpty().MaximumLength(200);
        RuleFor(comando => comando.ValorInicial).GreaterThan(0);
        RuleFor(comando => comando.ValorResidual)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(comando => comando.ValorInicial);
        RuleFor(comando => comando.VidaUtilMeses).GreaterThan(0);
        RuleFor(comando => comando.Placa).NotEmpty().Must(Placa.EhValida).WithMessage("Placa inválida.");
        RuleFor(comando => comando.Renavam).NotEmpty().Must(Renavam.EhValido).WithMessage("RENAVAM inválido.");
    }
}

/// <summary>Handler da incorporação de veículo.</summary>
public sealed class IncorporarVeiculoHandler(
    IVeiculoRepository veiculos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<IncorporarVeiculoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(IncorporarVeiculoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var placa = Placa.Criar(request.Placa);
        var renavam = Renavam.Criar(request.Renavam);

        if (await veiculos.ExisteRenavamAsync(renavam, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"RENAVAM já cadastrado no tenant: {renavam}.");
        }

        var veiculo = Veiculo.IncorporarVeiculo(
            tenant.TenantId,
            request.Descricao,
            ValorMonetario.De(request.ValorInicial),
            ValorMonetario.De(request.ValorResidual),
            request.VidaUtilMeses,
            request.DataIncorporacao,
            request.Origem,
            placa,
            renavam,
            Odometro.De(request.OdometroInicial),
            Horimetro.De(request.HorimetroInicial));

        veiculos.Adicionar(veiculo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return veiculo.Id.Value;
    }
}
