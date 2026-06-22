using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Resumo de uma pendência de licenciamento para leitura.</summary>
/// <param name="VeiculoId">Veículo.</param>
/// <param name="Placa">Placa do veículo.</param>
/// <param name="Exercicio">Exercício (ano).</param>
/// <param name="ValorIpva">Valor de IPVA conhecido para o exercício.</param>
/// <param name="Situacao">Situação do licenciamento.</param>
public sealed record LicenciamentoResumo(
    Guid VeiculoId,
    string Placa,
    int Exercicio,
    decimal ValorIpva,
    string Situacao);

/// <summary>Lista os veículos sem licenciamento regular no exercício (tenant-scoped, I-11).</summary>
/// <param name="Exercicio">Exercício (ano) a verificar.</param>
public sealed record ListarLicenciamentosPendentesQuery(int Exercicio)
    : IQuery<IReadOnlyList<LicenciamentoResumo>>;

/// <summary>Handler da consulta de licenciamentos pendentes.</summary>
public sealed class ListarLicenciamentosPendentesHandler(IVeiculoRepository veiculos)
    : IQueryHandler<ListarLicenciamentosPendentesQuery, IReadOnlyList<LicenciamentoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<LicenciamentoResumo>> Handle(
        ListarLicenciamentosPendentesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pendentes = await veiculos
            .ListarLicenciamentosPendentesAsync(request.Exercicio, cancellationToken)
            .ConfigureAwait(false);

        return pendentes
            .Select(pendente => new LicenciamentoResumo(
                pendente.VeiculoId,
                pendente.Placa,
                pendente.Exercicio,
                pendente.ValorIpva,
                pendente.Situacao))
            .ToList();
    }
}
