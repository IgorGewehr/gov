using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;

namespace Tensorroot.Gov.Modules.Financas.Application.Liquidacoes;

/// <summary>Resumo de uma liquidação para leitura.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="EmpenhoId">Empenho vinculado.</param>
/// <param name="Valor">Valor liquidado.</param>
/// <param name="ValorPago">Valor pago.</param>
/// <param name="SaldoAPagar">Saldo a pagar.</param>
/// <param name="DataLiquidacao">Data da liquidação.</param>
/// <param name="Documento">Documento comprobatório (texto).</param>
/// <param name="Situacao">Situação atual.</param>
public sealed record LiquidacaoResumo(
    Guid Id,
    Guid EmpenhoId,
    decimal Valor,
    decimal ValorPago,
    decimal SaldoAPagar,
    DateOnly DataLiquidacao,
    string Documento,
    string Situacao);

/// <summary>Obtém uma liquidação por identificador.</summary>
/// <param name="LiquidacaoId">Identificador.</param>
public sealed record ObterLiquidacaoQuery(Guid LiquidacaoId) : IQuery<LiquidacaoResumo?>;

/// <summary>Lista as liquidações de um empenho.</summary>
/// <param name="EmpenhoId">Empenho.</param>
public sealed record ListarLiquidacoesPorEmpenhoQuery(Guid EmpenhoId) : IQuery<IReadOnlyList<LiquidacaoResumo>>;

/// <summary>Mapeamento de domínio para resumo de liquidação.</summary>
internal static class LiquidacaoResumoMapper
{
    public static LiquidacaoResumo Mapear(Liquidacao l) => new(
        l.Id.Value,
        l.EmpenhoId.Value,
        l.Valor.Valor,
        l.ValorPago.Valor,
        l.SaldoAPagar.Valor,
        l.DataLiquidacao,
        l.Documento.ToString(),
        l.Situacao.ToString());
}

/// <summary>Handler da consulta de liquidação.</summary>
public sealed class ObterLiquidacaoHandler(ILiquidacaoRepository liquidacoes)
    : IQueryHandler<ObterLiquidacaoQuery, LiquidacaoResumo?>
{
    /// <inheritdoc />
    public async Task<LiquidacaoResumo?> Handle(ObterLiquidacaoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var liquidacao = await liquidacoes.ObterPorIdAsync(new LiquidacaoId(request.LiquidacaoId), cancellationToken).ConfigureAwait(false);
        return liquidacao is null ? null : LiquidacaoResumoMapper.Mapear(liquidacao);
    }
}

/// <summary>Handler da listagem de liquidações por empenho.</summary>
public sealed class ListarLiquidacoesPorEmpenhoHandler(ILiquidacaoRepository liquidacoes)
    : IQueryHandler<ListarLiquidacoesPorEmpenhoQuery, IReadOnlyList<LiquidacaoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<LiquidacaoResumo>> Handle(
        ListarLiquidacoesPorEmpenhoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontradas = await liquidacoes.ListarPorEmpenhoAsync(new EmpenhoId(request.EmpenhoId), cancellationToken).ConfigureAwait(false);
        return encontradas.Select(LiquidacaoResumoMapper.Mapear).ToList();
    }
}
