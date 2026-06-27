using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;

namespace Tensorroot.Gov.Modules.Financas.Application.Liquidacoes;

/// <summary>Retenção apurada sobre a liquidação (para leitura).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Natureza">Natureza (texto do enum).</param>
/// <param name="Valor">Valor retido.</param>
/// <param name="CodigoReceita">Código de receita (DARF/guia).</param>
/// <param name="Recolhida">Se já recolhida.</param>
public sealed record RetencaoResumo(Guid Id, string Natureza, decimal Valor, string? CodigoReceita, bool Recolhida);

/// <summary>Resumo de uma liquidação para leitura.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="EmpenhoId">Empenho vinculado.</param>
/// <param name="Valor">Valor liquidado (bruto).</param>
/// <param name="ValorPago">Valor pago.</param>
/// <param name="SaldoAPagar">Saldo a pagar.</param>
/// <param name="TotalRetido">Total retido (consignações).</param>
/// <param name="ValorLiquido">Valor líquido a pagar ao credor.</param>
/// <param name="DataLiquidacao">Data da liquidação.</param>
/// <param name="Documento">Documento comprobatório (texto).</param>
/// <param name="Situacao">Situação atual.</param>
/// <param name="Retencoes">Retenções apuradas.</param>
public sealed record LiquidacaoResumo(
    Guid Id,
    Guid EmpenhoId,
    decimal Valor,
    decimal ValorPago,
    decimal SaldoAPagar,
    decimal TotalRetido,
    decimal ValorLiquido,
    DateOnly DataLiquidacao,
    string Documento,
    string Situacao,
    IReadOnlyList<RetencaoResumo> Retencoes);

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
        l.TotalRetido.Valor,
        l.ValorLiquido.Valor,
        l.DataLiquidacao,
        l.Documento.ToString(),
        l.Situacao.ToString(),
        l.Retencoes.Select(r => new RetencaoResumo(
            r.Id.Value,
            r.Natureza.ToString(),
            r.Valor.Valor,
            r.CodigoReceita,
            r.Recolhida)).ToList());
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
