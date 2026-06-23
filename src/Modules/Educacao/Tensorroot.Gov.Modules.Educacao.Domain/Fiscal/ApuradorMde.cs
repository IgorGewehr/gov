namespace Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;

/// <summary>Linha de despesa de Educação a apurar (funcional + fonte + valor executado).</summary>
/// <param name="Codigo">Funcional resolvida (função/subfunção) da despesa.</param>
/// <param name="FonteRecurso">Fonte/destinação de recurso (PCASP), opcional.</param>
/// <param name="Valor">Valor executado da despesa (&gt;= 0).</param>
public readonly record struct DespesaEducacao(CodigoFuncionalEducacao Codigo, string? FonteRecurso, decimal Valor);

/// <summary>
/// <b>E-1 — ApuradorMde.</b> Especialização (refinamento) do <c>ApuradorMinimo</c> do M7.0 para a
/// <b>Manutenção e Desenvolvimento do Ensino</b> (CF art. 212; LDB Lei 9.394/1996 arts. 70/71). Onde o
/// apurador genérico recebe a despesa "já classificada", o ApuradorMde faz a <b>classificação FINA</b>
/// dela: aplica o <see cref="ClassificadorMde"/> a cada despesa de Educação, soma <b>apenas o que computa</b>
/// (art. 70) e descarta o que não computa (art. 71 — merenda, assistência médico-odontológica, pesquisa
/// não vinculada ao ensino, obras de infraestrutura urbana fora das escolas, inativos...), e então apura o
/// indicador contra o mínimo vigente (default 25%, parametrizável). <b>Espelha o ApuradorAsps da Saúde.</b>
/// <para>
/// A aferição do mínimo de 25% é <b>ANUAL</b> (encerramento do exercício), distinta do indicador
/// bimestral de acompanhamento — o <see cref="NaturezaAferimentoMde"/> carimba o resultado para que o
/// consumidor não confunda os dois (E-1, RISCO #3). Puro e <b>reprodutível</b> (sem relógio nem estado):
/// mesmas despesas + regras + receita-base + % ⇒ mesmo <see cref="IndicadorMde"/>. As regras chegam já
/// filtradas por vigência pelo chamador (Via A2).
/// </para>
/// // TODO(validar-oficial): a classificação fina MDE (inclusões/exclusões da LDB arts. 70/71 por
/// subfunção/fonte) e o mapeamento contas→campos SIOPE seguem o seed default até o <b>Manual SIOPE</b>
/// vigente — ver <see cref="RegraClassificacaoMde"/>.
/// </summary>
public static class ApuradorMde
{
    /// <summary>
    /// Apura o total aplicado em MDE a partir das despesas de Educação e das regras vigentes (só soma o
    /// que <see cref="ClassificadorMde.ComputaNaMde"/> aceita).
    /// </summary>
    /// <param name="despesas">Despesas de Educação do período (funcional/fonte/valor).</param>
    /// <param name="regras">Regras de classificação MDE vigentes do tenant.</param>
    /// <returns>Total aplicado em MDE (despesa computável).</returns>
    public static decimal ApurarAplicadoComputavel(
        IReadOnlyCollection<DespesaEducacao> despesas,
        IReadOnlyCollection<RegraClassificacaoMde> regras)
    {
        ArgumentNullException.ThrowIfNull(despesas);
        ArgumentNullException.ThrowIfNull(regras);

        decimal aplicado = 0m;
        foreach (var despesa in despesas)
        {
            if (ClassificadorMde.ComputaNaMde(despesa.Codigo, despesa.FonteRecurso, regras))
            {
                aplicado += despesa.Valor;
            }
        }

        return aplicado;
    }

    /// <summary>
    /// Apura o indicador MDE de um período: classifica e soma a despesa computável e a confronta com a
    /// receita-base e o mínimo vigente, carimbando a natureza (anual de conformidade / bimestral).
    /// </summary>
    /// <param name="natureza">Natureza da apuração (anual de conformidade / bimestral informativo).</param>
    /// <param name="receitaBase">Receita-base (impostos + transferências constitucionais), &gt;= 0.</param>
    /// <param name="despesas">Despesas de Educação do período.</param>
    /// <param name="regras">Regras de classificação MDE vigentes do tenant.</param>
    /// <param name="percentualMinimo">Percentual mínimo vigente (0..1; default legal 25%), parametrizável.</param>
    /// <returns>Indicador MDE apurado.</returns>
    public static IndicadorMde Apurar(
        NaturezaAferimentoMde natureza,
        decimal receitaBase,
        IReadOnlyCollection<DespesaEducacao> despesas,
        IReadOnlyCollection<RegraClassificacaoMde> regras,
        decimal percentualMinimo)
    {
        var aplicado = ApurarAplicadoComputavel(despesas, regras);
        return IndicadorMde.Apurar(natureza, receitaBase, aplicado, percentualMinimo);
    }
}
