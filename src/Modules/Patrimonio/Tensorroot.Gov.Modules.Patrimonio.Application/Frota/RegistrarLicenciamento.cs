using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Registra o licenciamento anual/IPVA de um veículo para um exercício.</summary>
/// <param name="VeiculoId">Veículo licenciado.</param>
/// <param name="Exercicio">Exercício (ano).</param>
/// <param name="ValorIpva">Valor do IPVA.</param>
/// <param name="ValorTaxa">Valor da taxa de licenciamento.</param>
/// <param name="Data">Data do licenciamento.</param>
public sealed record RegistrarLicenciamentoCommand(
    Guid VeiculoId,
    int Exercicio,
    decimal ValorIpva,
    decimal ValorTaxa,
    DateOnly Data) : ICommand;

/// <summary>Handler do registro de licenciamento.</summary>
public sealed class RegistrarLicenciamentoHandler(IVeiculoRepository veiculos, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarLicenciamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarLicenciamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var veiculo = await veiculos.ObterPorIdAsync(new VeiculoId(request.VeiculoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Veículo não encontrado.");

        veiculo.RegistrarLicenciamento(
            request.Exercicio,
            ValorMonetario.De(request.ValorIpva),
            ValorMonetario.De(request.ValorTaxa),
            request.Data);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
