using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Resumo de uma ordem de serviço de manutenção em aberto (Painel de Frota — Onda 3a).</summary>
/// <param name="VeiculoId">Veículo.</param>
/// <param name="Placa">Placa do veículo.</param>
/// <param name="OrdemServicoId">Identificador da ordem de serviço.</param>
/// <param name="Descricao">Descrição do serviço.</param>
/// <param name="CustoEstimado">Custo estimado na abertura.</param>
/// <param name="Odometro">Leitura do odômetro na abertura.</param>
public sealed record ManutencaoAbertaResumo(
    Guid VeiculoId,
    string Placa,
    Guid OrdemServicoId,
    string Descricao,
    decimal CustoEstimado,
    int Odometro);

/// <summary>Lista as ordens de serviço de manutenção em aberto de toda a frota do tenant (tenant-scoped).</summary>
public sealed record ListarManutencoesAbertasQuery
    : IQuery<IReadOnlyList<ManutencaoAbertaResumo>>;

/// <summary>Handler da consulta de manutenções abertas.</summary>
public sealed class ListarManutencoesAbertasHandler(IVeiculoRepository veiculos)
    : IQueryHandler<ListarManutencoesAbertasQuery, IReadOnlyList<ManutencaoAbertaResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ManutencaoAbertaResumo>> Handle(
        ListarManutencoesAbertasQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var linhas = await veiculos
            .ListarManutencoesAbertasAsync(cancellationToken)
            .ConfigureAwait(false);

        return linhas
            .Select(linha => new ManutencaoAbertaResumo(
                linha.VeiculoId,
                linha.Placa,
                linha.OrdemServicoId,
                linha.Descricao,
                linha.CustoEstimado,
                linha.Odometro))
            .ToList();
    }
}
