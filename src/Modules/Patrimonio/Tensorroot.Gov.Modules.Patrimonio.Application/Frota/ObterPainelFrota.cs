using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>KPIs agregados da frota em um período (Painel de Frota — Onda 3a).</summary>
/// <param name="De">Data inicial do período (inclusiva).</param>
/// <param name="Ate">Data final do período (inclusiva).</param>
/// <param name="QuantidadeVeiculos">Quantidade de veículos com custo no período.</param>
/// <param name="GastoCombustivel">Gasto total com combustível no período.</param>
/// <param name="GastoManutencao">Gasto total com manutenção (OS concluídas) no período.</param>
/// <param name="GastoMultas">Valor total de multas (infrações) no período.</param>
/// <param name="GastoTotal">Gasto total da frota (combustível + manutenção + multas).</param>
/// <param name="LitrosTotais">Litros totais abastecidos no período.</param>
/// <param name="ConsumoMedioFrotaKmL">Consumo médio da frota km/L (nulo se indeterminável).</param>
/// <param name="ManutencoesAbertas">Quantidade de ordens de serviço em aberto na frota.</param>
/// <param name="Veiculos">Detalhe de custo/consumo por veículo no período (ordenado por custo total desc.).</param>
public sealed record PainelFrotaResumo(
    DateOnly De,
    DateOnly Ate,
    int QuantidadeVeiculos,
    decimal GastoCombustivel,
    decimal GastoManutencao,
    decimal GastoMultas,
    decimal GastoTotal,
    decimal LitrosTotais,
    decimal? ConsumoMedioFrotaKmL,
    int ManutencoesAbertas,
    IReadOnlyList<CustoVeiculoResumo> Veiculos);

/// <summary>
/// Painel agregado da frota em um período (tenant-scoped): gasto total combustível + manutenção +
/// multas, consumo médio km/L e detalhamento por veículo. Puro read model sobre o agregado Veiculo.
/// </summary>
/// <param name="De">Data inicial (inclusiva).</param>
/// <param name="Ate">Data final (inclusiva).</param>
public sealed record ObterPainelFrotaQuery(DateOnly De, DateOnly Ate)
    : IQuery<PainelFrotaResumo>;

/// <summary>Handler do painel de frota.</summary>
public sealed class ObterPainelFrotaHandler(IVeiculoRepository veiculos)
    : IQueryHandler<ObterPainelFrotaQuery, PainelFrotaResumo>
{
    /// <inheritdoc />
    public async Task<PainelFrotaResumo> Handle(
        ObterPainelFrotaQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var linhas = await veiculos
            .ProjetarCustosPorVeiculoAsync(request.De, request.Ate, cancellationToken)
            .ConfigureAwait(false);

        var manutencoesAbertas = await veiculos
            .ListarManutencoesAbertasAsync(cancellationToken)
            .ConfigureAwait(false);

        // Considera no painel apenas os veículos que tiveram algum custo no período.
        var comCusto = linhas.Where(l => l.CustoTotal > 0m || l.LitrosAbastecidos > 0m).ToList();

        var gastoCombustivel = comCusto.Sum(l => l.GastoCombustivel);
        var gastoManutencao = comCusto.Sum(l => l.GastoManutencao);
        var gastoMultas = comCusto.Sum(l => l.GastoMultas);
        var litrosTotais = comCusto.Sum(l => l.LitrosAbastecidos);
        var kmTotais = comCusto.Sum(l => l.KmRodados);

        decimal? consumoFrota = litrosTotais > 0m && kmTotais > 0
            ? Math.Round(kmTotais / litrosTotais, 2)
            : null;

        var detalhe = comCusto
            .Select(l => new CustoVeiculoResumo(
                l.VeiculoId,
                l.Placa,
                l.Descricao,
                l.GastoCombustivel,
                l.LitrosAbastecidos,
                l.GastoManutencao,
                l.GastoMultas,
                l.CustoTotal,
                l.KmRodados,
                l.ConsumoMedioKmL))
            .ToList();

        return new PainelFrotaResumo(
            request.De,
            request.Ate,
            comCusto.Count,
            gastoCombustivel,
            gastoManutencao,
            gastoMultas,
            gastoCombustivel + gastoManutencao + gastoMultas,
            litrosTotais,
            consumoFrota,
            manutencoesAbertas.Count,
            detalhe);
    }
}
