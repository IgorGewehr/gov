namespace Tensorroot.Gov.SharedKernel.Tempo;

/// <summary>
/// Calendario de DIAS UTEIS por tenant: conhece fins de semana, feriados nacionais FIXOS (lei) e
/// MOVEIS (Pascoa/Carnaval/Corpus Christi via Computus), e feriados MUNICIPAIS/pontos facultativos
/// parametrizados por tenant. Porta de DOMINIO (injetada nas transicoes do agregado).
/// DETERMINISTICA: nao usa relogio interno — recebe sempre a data base (CLAUDE.md S7/S16). Espelha a
/// filosofia de <c>IRegraAfastamentoProvider</c>/<c>ICalendarioFiscal</c>: o prazo e CALCULADO, nunca
/// digitado; a fonte dos feriados fica fora do dominio (na Infra, por tenant).
/// </summary>
public interface ICalendarioDiasUteis
{
    /// <summary>
    /// Soma <paramref name="diasUteis"/> dias uteis a partir de <paramref name="inicio"/>, pulando fins
    /// de semana e feriados do tenant. A contagem comeca no PROXIMO dia util (o <paramref name="inicio"/>
    /// NAO conta), convencao do PNCP/processo administrativo (a publicacao da data inicial nao consome
    /// prazo). <c>AdicionarDiasUteis(x, 0) == x</c>.
    /// </summary>
    /// <param name="inicio">Data base (nao consome prazo).</param>
    /// <param name="diasUteis">Quantidade de dias uteis a somar (&gt;= 0).</param>
    /// <returns>Data resultante (sempre dia util quando <paramref name="diasUteis"/> &gt; 0).</returns>
    DateOnly AdicionarDiasUteis(DateOnly inicio, int diasUteis);

    /// <summary>Verdadeiro se <paramref name="data"/> e dia util (nao e fim de semana nem feriado do tenant).</summary>
    /// <param name="data">Data a avaliar.</param>
    /// <returns><c>true</c> se for dia util.</returns>
    bool EhDiaUtil(DateOnly data);

    /// <summary>
    /// Proximo dia util &gt;= <paramref name="data"/> (devolve a propria data se ja for util — idempotente).
    /// Base para a regra "vencimento que cai em feriado/fim de semana rola para o proximo util"
    /// (Lei 14.133 art. 110 / art. 224 CPC).
    /// </summary>
    /// <param name="data">Data base.</param>
    /// <returns>Proximo dia util (inclusivo).</returns>
    DateOnly ProximoDiaUtil(DateOnly data);

    /// <summary>
    /// Quantidade de dias uteis no intervalo entre <paramref name="a"/> e <paramref name="b"/>
    /// (exclusivo no inicio, inclusivo no fim — dias uteis "decorridos"). Negativo se <paramref name="b"/>
    /// &lt; <paramref name="a"/>.
    /// </summary>
    /// <param name="a">Limite inicial (exclusivo).</param>
    /// <param name="b">Limite final (inclusivo).</param>
    /// <returns>Numero de dias uteis no intervalo (negativo se invertido).</returns>
    int DiasUteisEntre(DateOnly a, DateOnly b);
}
