using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Abre uma ordem de serviço de manutenção para um veículo, retornando seu identificador.</summary>
/// <param name="VeiculoId">Veículo em manutenção.</param>
/// <param name="Descricao">Descrição do serviço.</param>
/// <param name="CustoEstimado">Custo estimado.</param>
/// <param name="Odometro">Leitura do odômetro na abertura.</param>
public sealed record AbrirOrdemServicoCommand(
    Guid VeiculoId,
    string Descricao,
    decimal CustoEstimado,
    int Odometro) : ICommand<Guid>;

/// <summary>Handler da abertura de ordem de serviço de manutenção.</summary>
public sealed class AbrirOrdemServicoHandler(IVeiculoRepository veiculos, IUnitOfWork unitOfWork)
    : ICommandHandler<AbrirOrdemServicoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirOrdemServicoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var veiculo = await veiculos.ObterPorIdAsync(new VeiculoId(request.VeiculoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Veículo não encontrado.");

        var ordemServicoId = veiculo.AbrirOrdemServico(
            request.Descricao,
            ValorMonetario.De(request.CustoEstimado),
            request.Odometro);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ordemServicoId.Value;
    }
}
