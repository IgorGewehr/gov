using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;

namespace Tensorroot.Gov.Modules.Financas.Application.Dotacoes;

/// <summary>Resumo de uma dotação orçamentária para leitura.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Exercicio">Exercício.</param>
/// <param name="Classificacao">Classificação orçamentária (texto).</param>
/// <param name="ValorDotadoInicial">Dotação inicial.</param>
/// <param name="ValorReforcado">Reforços acumulados.</param>
/// <param name="ValorAnulado">Anulações de crédito.</param>
/// <param name="ValorAtualizado">Dotação atualizada.</param>
/// <param name="ValorEmpenhadoLiquido">Empenhado líquido.</param>
/// <param name="SaldoDisponivel">Saldo disponível para empenho.</param>
/// <param name="Situacao">Situação atual.</param>
public sealed record DotacaoResumo(
    Guid Id,
    int Exercicio,
    string Classificacao,
    decimal ValorDotadoInicial,
    decimal ValorReforcado,
    decimal ValorAnulado,
    decimal ValorAtualizado,
    decimal ValorEmpenhadoLiquido,
    decimal SaldoDisponivel,
    string Situacao);

/// <summary>Obtém uma dotação por identificador.</summary>
/// <param name="DotacaoId">Identificador.</param>
public sealed record ObterDotacaoQuery(Guid DotacaoId) : IQuery<DotacaoResumo?>;

/// <summary>Lista as dotações de um exercício.</summary>
/// <param name="Exercicio">Exercício.</param>
public sealed record ListarDotacoesPorExercicioQuery(int Exercicio) : IQuery<IReadOnlyList<DotacaoResumo>>;

/// <summary>Mapeamento de domínio para resumo de dotação.</summary>
internal static class DotacaoResumoMapper
{
    public static DotacaoResumo Mapear(DotacaoOrcamentaria d) => new(
        d.Id.Value,
        d.Exercicio,
        d.Classificacao.ParaTexto(),
        d.ValorDotadoInicial.Valor,
        d.ValorReforcado.Valor,
        d.ValorAnulado.Valor,
        d.ValorAtualizado.Valor,
        d.ValorEmpenhadoLiquido.Valor,
        d.SaldoDisponivel.Valor,
        d.Situacao.ToString());
}

/// <summary>Handler da consulta de dotação.</summary>
public sealed class ObterDotacaoHandler(IDotacaoOrcamentariaRepository dotacoes)
    : IQueryHandler<ObterDotacaoQuery, DotacaoResumo?>
{
    /// <inheritdoc />
    public async Task<DotacaoResumo?> Handle(ObterDotacaoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dotacao = await dotacoes.ObterPorIdAsync(new DotacaoOrcamentariaId(request.DotacaoId), cancellationToken).ConfigureAwait(false);
        return dotacao is null ? null : DotacaoResumoMapper.Mapear(dotacao);
    }
}

/// <summary>Handler da listagem de dotações por exercício.</summary>
public sealed class ListarDotacoesPorExercicioHandler(IDotacaoOrcamentariaRepository dotacoes)
    : IQueryHandler<ListarDotacoesPorExercicioQuery, IReadOnlyList<DotacaoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DotacaoResumo>> Handle(
        ListarDotacoesPorExercicioQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontradas = await dotacoes.ListarPorExercicioAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);
        return encontradas.Select(DotacaoResumoMapper.Mapear).ToList();
    }
}
