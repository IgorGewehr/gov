using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Frota;

/// <summary>Resumo de uma CNH a vencer/vencida (Painel de Frota — Onda 3a).</summary>
/// <param name="VeiculoId">Veículo vinculado.</param>
/// <param name="Placa">Placa do veículo.</param>
/// <param name="MotoristaId">Identificador do motorista.</param>
/// <param name="Nome">Nome do condutor.</param>
/// <param name="Cnh">Número da CNH.</param>
/// <param name="CategoriaCnh">Categoria da CNH.</param>
/// <param name="ValidadeCnh">Validade da CNH.</param>
/// <param name="DiasParaVencer">Dias até o vencimento (negativo se já vencida).</param>
/// <param name="Vencida">Indica se a CNH já está vencida na data de referência.</param>
public sealed record CnhVencendoResumo(
    Guid VeiculoId,
    string Placa,
    Guid MotoristaId,
    string Nome,
    string Cnh,
    string CategoriaCnh,
    DateOnly ValidadeCnh,
    int DiasParaVencer,
    bool Vencida);

/// <summary>
/// Lista condutores com CNH a vencer dentro de uma janela de dias a partir de hoje (tenant-scoped),
/// incluindo CNH já vencida. Suporta a busca ativa de regularização (CTB).
/// </summary>
/// <param name="Dias">Janela em dias (a partir de hoje). Saneada para não-negativa.</param>
public sealed record ListarCnhVencendoQuery(int Dias)
    : IQuery<IReadOnlyList<CnhVencendoResumo>>;

/// <summary>Handler da consulta de CNH a vencer.</summary>
public sealed class ListarCnhVencendoHandler(IVeiculoRepository veiculos)
    : IQueryHandler<ListarCnhVencendoQuery, IReadOnlyList<CnhVencendoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CnhVencendoResumo>> Handle(
        ListarCnhVencendoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var dias = Math.Max(0, request.Dias);
        var ate = hoje.AddDays(dias);

        var linhas = await veiculos
            .ListarCnhVencendoAsync(hoje, ate, cancellationToken)
            .ConfigureAwait(false);

        return linhas
            .Select(linha => new CnhVencendoResumo(
                linha.VeiculoId,
                linha.Placa,
                linha.MotoristaId,
                linha.Nome,
                linha.Cnh,
                linha.CategoriaCnh,
                linha.ValidadeCnh,
                linha.DiasParaVencer,
                linha.DiasParaVencer < 0))
            .ToList();
    }
}
