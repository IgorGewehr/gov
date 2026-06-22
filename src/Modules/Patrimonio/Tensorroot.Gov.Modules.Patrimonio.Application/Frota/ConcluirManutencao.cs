using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Conclui uma ordem de serviço de manutenção de um veículo.</summary>
/// <param name="VeiculoId">Veículo da ordem de serviço.</param>
/// <param name="OrdemServicoId">Ordem de serviço a concluir.</param>
/// <param name="CustoRealizado">Custo realizado.</param>
/// <param name="DataConclusao">Data de conclusão.</param>
public sealed record ConcluirManutencaoCommand(
    Guid VeiculoId,
    Guid OrdemServicoId,
    decimal CustoRealizado,
    DateOnly DataConclusao) : ICommand;

/// <summary>Handler da conclusão de manutenção.</summary>
public sealed class ConcluirManutencaoHandler(IVeiculoRepository veiculos, IUnitOfWork unitOfWork)
    : ICommandHandler<ConcluirManutencaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ConcluirManutencaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var veiculo = await veiculos.ObterPorIdAsync(new VeiculoId(request.VeiculoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Veículo não encontrado.");

        veiculo.ConcluirManutencao(
            new ManutencaoOsId(request.OrdemServicoId),
            ValorMonetario.De(request.CustoRealizado),
            request.DataConclusao);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
