using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>
/// Tomba um veículo recém-incorporado, atribuindo número de tombo e ativando-o no acervo
/// (transição <c>EmIncorporacao → Tombado</c>). É o gate que habilita as operações de frota
/// (abastecimento, ordem de serviço, multa, licenciamento) — espelha o tombamento do bem.
/// </summary>
/// <param name="VeiculoId">Veículo a tombar.</param>
/// <param name="NumeroTombamento">Número de tombamento (placa patrimonial).</param>
public sealed record TombarVeiculoCommand(Guid VeiculoId, string NumeroTombamento) : ICommand;

/// <summary>Regras de validação do tombamento de veículo.</summary>
public sealed class TombarVeiculoValidator : AbstractValidator<TombarVeiculoCommand>
{
    /// <summary>Define as regras.</summary>
    public TombarVeiculoValidator()
    {
        RuleFor(comando => comando.VeiculoId).NotEmpty();
        RuleFor(comando => comando.NumeroTombamento).NotEmpty().MaximumLength(40);
    }
}

/// <summary>Handler do tombamento de veículo.</summary>
public sealed class TombarVeiculoHandler(IVeiculoRepository veiculos, IUnitOfWork unitOfWork)
    : ICommandHandler<TombarVeiculoCommand>
{
    /// <inheritdoc />
    public async Task Handle(TombarVeiculoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var veiculo = await veiculos.ObterPorIdAsync(new VeiculoId(request.VeiculoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Veículo não encontrado.");

        veiculo.Tombar(request.NumeroTombamento);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
