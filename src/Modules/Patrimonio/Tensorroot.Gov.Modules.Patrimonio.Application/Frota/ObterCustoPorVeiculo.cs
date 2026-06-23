using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Custo e consumo de um veículo em um período (Painel de Frota — Onda 3a).</summary>
/// <param name="VeiculoId">Identificador do veículo.</param>
/// <param name="Placa">Placa do veículo.</param>
/// <param name="Descricao">Descrição do veículo.</param>
/// <param name="GastoCombustivel">Gasto com combustível no período.</param>
/// <param name="LitrosAbastecidos">Litros abastecidos no período.</param>
/// <param name="GastoManutencao">Gasto com manutenção (OS concluídas) no período.</param>
/// <param name="GastoMultas">Valor de multas (infrações) no período.</param>
/// <param name="CustoTotal">Custo total no período (combustível + manutenção + multas).</param>
/// <param name="KmRodados">Quilômetros rodados no período.</param>
/// <param name="ConsumoMedioKmL">Consumo médio km/L no período (nulo se indeterminável).</param>
public sealed record CustoVeiculoResumo(
    Guid VeiculoId,
    string Placa,
    string Descricao,
    decimal GastoCombustivel,
    decimal LitrosAbastecidos,
    decimal GastoManutencao,
    decimal GastoMultas,
    decimal CustoTotal,
    int KmRodados,
    decimal? ConsumoMedioKmL);

/// <summary>
/// Consulta o custo/consumo de um veículo específico em um período (tenant-scoped).
/// Reusa a projeção da frota e filtra pelo veículo informado.
/// </summary>
/// <param name="VeiculoId">Veículo.</param>
/// <param name="De">Data inicial (inclusiva).</param>
/// <param name="Ate">Data final (inclusiva).</param>
public sealed record ObterCustoPorVeiculoQuery(Guid VeiculoId, DateOnly De, DateOnly Ate)
    : IQuery<CustoVeiculoResumo?>;

/// <summary>Handler da consulta de custo por veículo.</summary>
public sealed class ObterCustoPorVeiculoHandler(IVeiculoRepository veiculos)
    : IQueryHandler<ObterCustoPorVeiculoQuery, CustoVeiculoResumo?>
{
    /// <inheritdoc />
    public async Task<CustoVeiculoResumo?> Handle(
        ObterCustoPorVeiculoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var linhas = await veiculos
            .ProjetarCustosPorVeiculoAsync(request.De, request.Ate, cancellationToken)
            .ConfigureAwait(false);

        return linhas
            .Where(linha => linha.VeiculoId == request.VeiculoId)
            .Select(linha => new CustoVeiculoResumo(
                linha.VeiculoId,
                linha.Placa,
                linha.Descricao,
                linha.GastoCombustivel,
                linha.LitrosAbastecidos,
                linha.GastoManutencao,
                linha.GastoMultas,
                linha.CustoTotal,
                linha.KmRodados,
                linha.ConsumoMedioKmL))
            .FirstOrDefault();
    }
}
