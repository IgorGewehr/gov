using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Resumo de um abastecimento para leitura.</summary>
/// <param name="Id">Identificador do abastecimento.</param>
/// <param name="Data">Data do abastecimento.</param>
/// <param name="Litros">Litros abastecidos.</param>
/// <param name="Valor">Valor do abastecimento.</param>
/// <param name="Odometro">Leitura do odômetro no ato.</param>
public sealed record AbastecimentoResumo(
    Guid Id,
    DateOnly Data,
    decimal Litros,
    decimal Valor,
    int Odometro);

/// <summary>Lista os abastecimentos de um veículo em um período (tenant-scoped).</summary>
/// <param name="VeiculoId">Veículo.</param>
/// <param name="De">Data inicial (inclusiva).</param>
/// <param name="Ate">Data final (inclusiva).</param>
public sealed record ListarAbastecimentosDoVeiculoQuery(Guid VeiculoId, DateOnly De, DateOnly Ate)
    : IQuery<IReadOnlyList<AbastecimentoResumo>>;

/// <summary>Handler da consulta de abastecimentos do veículo.</summary>
public sealed class ListarAbastecimentosDoVeiculoHandler(IVeiculoRepository veiculos)
    : IQueryHandler<ListarAbastecimentosDoVeiculoQuery, IReadOnlyList<AbastecimentoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AbastecimentoResumo>> Handle(
        ListarAbastecimentosDoVeiculoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var abastecimentos = await veiculos
            .ListarAbastecimentosAsync(new VeiculoId(request.VeiculoId), request.De, request.Ate, cancellationToken)
            .ConfigureAwait(false);

        return abastecimentos
            .Select(abastecimento => new AbastecimentoResumo(
                abastecimento.Id.Value,
                abastecimento.Data,
                abastecimento.Litros,
                abastecimento.Valor.Valor,
                abastecimento.Odometro.Valor))
            .ToList();
    }
}
