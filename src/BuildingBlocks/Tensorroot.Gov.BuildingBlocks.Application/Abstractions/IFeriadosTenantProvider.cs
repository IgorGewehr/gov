namespace Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Fonte de feriados especificos do tenant (municipais, religiosos locais, pontos facultativos adotados)
/// e do conjunto NAO UTIL vigente para um ano. Parametrizavel por tenant (CLAUDE.md S7/S16). Espelha a
/// filosofia de <c>IRegraAfastamentoProvider</c>/<c>ICalendarioFiscal</c>: a porta vive na camada de
/// abstracao; a fonte (config hoje, tabela <c>core.CalendarioFeriado</c> depois) fica na Infra.
/// </summary>
public interface IFeriadosTenantProvider
{
    /// <summary>
    /// Conjunto de datas NAO UTEIS do tenant no <paramref name="ano"/>: nacionais fixos ∪ moveis
    /// (Computus) ∪ municipais ∪ pontos facultativos adotados. Reproduzivel: depende so de (tenant, ano).
    /// Resultado cacheavel por (tenant, ano).
    /// </summary>
    /// <param name="ano">Exercicio/ano de referencia.</param>
    /// <returns>Conjunto imutavel das datas nao uteis do tenant no ano.</returns>
    IReadOnlySet<DateOnly> FeriadosDoAno(int ano);
}
