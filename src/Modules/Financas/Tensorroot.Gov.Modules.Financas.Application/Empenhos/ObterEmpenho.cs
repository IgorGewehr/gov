using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;

namespace Tensorroot.Gov.Modules.Financas.Application.Empenhos;

/// <summary>Resumo de um empenho para leitura.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Numero">Número do empenho.</param>
/// <param name="DotacaoId">Dotação onerada.</param>
/// <param name="CredorNome">Nome do credor.</param>
/// <param name="CredorDocumento">Documento do credor.</param>
/// <param name="ValorEmpenhado">Valor empenhado.</param>
/// <param name="ValorAnulado">Valor anulado.</param>
/// <param name="ValorLiquidado">Valor liquidado.</param>
/// <param name="ValorPago">Valor pago.</param>
/// <param name="SaldoEmpenhado">Saldo empenhado.</param>
/// <param name="SaldoALiquidar">Saldo a liquidar.</param>
/// <param name="SaldoAPagar">Saldo a pagar.</param>
/// <param name="Situacao">Situação atual.</param>
/// <param name="Exercicio">Exercício.</param>
public sealed record EmpenhoResumo(
    Guid Id,
    string Numero,
    Guid DotacaoId,
    string CredorNome,
    string CredorDocumento,
    decimal ValorEmpenhado,
    decimal ValorAnulado,
    decimal ValorLiquidado,
    decimal ValorPago,
    decimal SaldoEmpenhado,
    decimal SaldoALiquidar,
    decimal SaldoAPagar,
    string Situacao,
    int Exercicio);

/// <summary>Obtém um empenho por identificador.</summary>
/// <param name="EmpenhoId">Identificador do empenho.</param>
public sealed record ObterEmpenhoQuery(Guid EmpenhoId) : IQuery<EmpenhoResumo?>;

/// <summary>Lista os empenhos que oneram uma dotação.</summary>
/// <param name="DotacaoId">Dotação.</param>
public sealed record ListarEmpenhosPorDotacaoQuery(Guid DotacaoId) : IQuery<IReadOnlyList<EmpenhoResumo>>;

/// <summary>Mapeamento de domínio para resumo de empenho.</summary>
internal static class EmpenhoResumoMapper
{
    public static EmpenhoResumo Mapear(Empenho e) => new(
        e.Id.Value,
        e.Numero,
        e.DotacaoId.Value,
        e.Credor.Nome,
        e.Credor.Documento,
        e.ValorEmpenhado.Valor,
        e.ValorAnulado.Valor,
        e.ValorLiquidado.Valor,
        e.ValorPago.Valor,
        e.SaldoEmpenhado.Valor,
        e.SaldoALiquidar.Valor,
        e.SaldoAPagar.Valor,
        e.Situacao.ToString(),
        e.Exercicio);
}

/// <summary>Handler da consulta de empenho.</summary>
public sealed class ObterEmpenhoHandler(IEmpenhoRepository empenhos)
    : IQueryHandler<ObterEmpenhoQuery, EmpenhoResumo?>
{
    /// <inheritdoc />
    public async Task<EmpenhoResumo?> Handle(ObterEmpenhoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var empenho = await empenhos.ObterPorIdAsync(new EmpenhoId(request.EmpenhoId), cancellationToken).ConfigureAwait(false);
        return empenho is null ? null : EmpenhoResumoMapper.Mapear(empenho);
    }
}

/// <summary>Handler da listagem de empenhos por dotação.</summary>
public sealed class ListarEmpenhosPorDotacaoHandler(IEmpenhoRepository empenhos)
    : IQueryHandler<ListarEmpenhosPorDotacaoQuery, IReadOnlyList<EmpenhoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EmpenhoResumo>> Handle(
        ListarEmpenhosPorDotacaoQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontrados = await empenhos.ListarPorDotacaoAsync(new DotacaoOrcamentariaId(request.DotacaoId), cancellationToken).ConfigureAwait(false);
        return encontrados.Select(EmpenhoResumoMapper.Mapear).ToList();
    }
}
