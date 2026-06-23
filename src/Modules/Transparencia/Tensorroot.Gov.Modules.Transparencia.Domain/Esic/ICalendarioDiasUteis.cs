namespace Tensorroot.Gov.Modules.Transparencia.Domain.Esic;

/// <summary>
/// Calendario de dias uteis para o calculo de prazos legais da LAI (20 dias + prorrogacao de 10),
/// parametrizavel por tenant (feriados municipais/estaduais/nacionais) — mesma filosofia de
/// <c>IRegraAfastamentoProvider</c>/<c>ICalendarioFiscal</c>: o prazo e CALCULADO, nunca digitado
/// (CLAUDE.md §7/§16 — prazos parametrizaveis, nada hardcoded). E uma porta de DOMINIO (passada ao
/// agregado nas transicoes), mantendo o calculo de prazo dentro da invariante do agregado, mas a fonte
/// dos feriados fora dele.
/// </summary>
public interface ICalendarioDiasUteis
{
    /// <summary>
    /// Soma <paramref name="diasUteis"/> dias uteis a uma data inicial, pulando fins de semana e
    /// feriados do tenant. Deterministico (sem relogio): o resultado depende so dos parametros.
    /// </summary>
    /// <param name="inicio">Data inicial (a contagem comeca no proximo dia util).</param>
    /// <param name="diasUteis">Quantidade de dias uteis a somar (&gt;= 0).</param>
    /// <returns>Data resultante (dia util).</returns>
    DateOnly SomarDiasUteis(DateOnly inicio, int diasUteis);
}
