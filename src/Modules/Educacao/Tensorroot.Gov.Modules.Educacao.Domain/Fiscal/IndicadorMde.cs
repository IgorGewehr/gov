using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;

/// <summary>Situação da aferição do mínimo de aplicação em MDE.</summary>
public enum SituacaoMde
{
    /// <summary>Percentual aplicado &gt;= mínimo: atingido.</summary>
    Atingido = 1,

    /// <summary>Percentual aplicado &lt; mínimo: não atingido.</summary>
    NaoAtingido = 2,
}

/// <summary>
/// Natureza da apuração MDE. <b>Separa explicitamente</b> o que é <b>conformidade</b> (a aferição que vale
/// para o TCE) do que é mero <b>acompanhamento</b> de execução — exigência do design (E-1, RISCO #3): o
/// mínimo de 25% é <b>aferido no encerramento do exercício</b> (anual); o indicador bimestral é só
/// informativo e não deve disparar alarme de "não atingido" nos bimestres iniciais (a despesa do ensino
/// concentra-se ao longo do ano). O cálculo é idêntico; o que muda é o <b>peso</b> da conclusão.
/// </summary>
public enum NaturezaAferimentoMde
{
    /// <summary>Aferição ANUAL de conformidade (encerramento do exercício) — a que vale para o TCE.</summary>
    AferimentoAnual = 1,

    /// <summary>Indicador BIMESTRAL de acompanhamento (informativo, não conformidade).</summary>
    IndicadorBimestral = 2,
}

/// <summary>
/// <b>E-1 — Indicador MDE (no espírito do SIOPE).</b> Resultado imutável e auto-calculado da apuração do
/// mínimo de Educação: a <b>receita-base</b> (impostos + transferências constitucionais — CF art. 212),
/// o <b>aplicado em MDE</b> (só a despesa de Educação classificada como computável — LDB art. 70), o
/// <b>percentual aplicado</b>, o <b>percentual mínimo vigente</b> (default legal 25% — CF art. 212,
/// parametrizável) e a <b>situação</b>. Carrega a <see cref="Natureza"/> (anual de conformidade vs.
/// bimestral de acompanhamento) para que o consumidor não confunda o indicador informativo com a
/// aferição oficial. Reprodutível (sem relógio): só depende das bases informadas.
/// </summary>
public sealed class IndicadorMde : ValueObject
{
    private IndicadorMde(
        NaturezaAferimentoMde natureza,
        decimal receitaBase,
        decimal aplicadoMde,
        decimal percentualAplicado,
        decimal percentualMinimo,
        SituacaoMde situacao)
    {
        Natureza = natureza;
        ReceitaBase = receitaBase;
        AplicadoMde = aplicadoMde;
        PercentualAplicado = percentualAplicado;
        PercentualMinimo = percentualMinimo;
        Situacao = situacao;
    }

    /// <summary>Natureza da apuração (aferição anual de conformidade vs. indicador bimestral informativo).</summary>
    public NaturezaAferimentoMde Natureza { get; }

    /// <summary>Receita-base do mínimo (impostos + transferências constitucionais — CF art. 212).</summary>
    public decimal ReceitaBase { get; }

    /// <summary>Valor aplicado em MDE (despesa de Educação computável, LDB art. 70).</summary>
    public decimal AplicadoMde { get; }

    /// <summary>Percentual aplicado = AplicadoMde / ReceitaBase (0..1, 4 casas), 0 quando base nula.</summary>
    public decimal PercentualAplicado { get; }

    /// <summary>Percentual mínimo vigente (default 0,25; Lei Orgânica pode fixar maior). Parametrizável.</summary>
    public decimal PercentualMinimo { get; }

    /// <summary>Situação da aferição.</summary>
    public SituacaoMde Situacao { get; }

    /// <summary>
    /// Indica se o mínimo de Educação foi atingido. <b>Só é conclusão de conformidade quando</b>
    /// <see cref="Natureza"/> é <see cref="NaturezaAferimentoMde.AferimentoAnual"/>; no bimestral é apenas
    /// um indicador informativo do andamento.
    /// </summary>
    public bool Atingido => Situacao == SituacaoMde.Atingido;

    /// <summary>Indica se este indicador é a aferição anual de conformidade (a que vale para o TCE).</summary>
    public bool EhConformidade => Natureza == NaturezaAferimentoMde.AferimentoAnual;

    /// <summary>Margem em pontos do percentual aplicado sobre o mínimo (negativa quando insuficiente).</summary>
    public decimal MargemPontos => PercentualAplicado - PercentualMinimo;

    /// <summary>
    /// Apura o indicador a partir da receita-base, do aplicado em MDE e do percentual mínimo vigente. O
    /// percentual aplicado é arredondado a 4 casas (precisão de aferição); a situação compara o aplicado
    /// já arredondado com o mínimo (evita falso "não atingido" por dízima).
    /// </summary>
    /// <param name="natureza">Natureza da apuração (anual de conformidade / bimestral informativo).</param>
    /// <param name="receitaBase">Receita-base (&gt;= 0).</param>
    /// <param name="aplicadoMde">Valor aplicado em MDE (&gt;= 0).</param>
    /// <param name="percentualMinimo">Percentual mínimo vigente (0..1).</param>
    /// <returns>Indicador apurado.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se algum valor for negativo ou o percentual fora de 0..1.</exception>
    public static IndicadorMde Apurar(
        NaturezaAferimentoMde natureza,
        decimal receitaBase,
        decimal aplicadoMde,
        decimal percentualMinimo)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(receitaBase);
        ArgumentOutOfRangeException.ThrowIfNegative(aplicadoMde);
        ArgumentOutOfRangeException.ThrowIfNegative(percentualMinimo);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentualMinimo, 1m);

        var percentualAplicado = receitaBase == 0m
            ? 0m
            : decimal.Round(aplicadoMde / receitaBase, 4, MidpointRounding.AwayFromZero);

        var situacao = percentualAplicado >= percentualMinimo
            ? SituacaoMde.Atingido
            : SituacaoMde.NaoAtingido;

        return new IndicadorMde(natureza, receitaBase, aplicadoMde, percentualAplicado, percentualMinimo, situacao);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Natureza;
        yield return ReceitaBase;
        yield return AplicadoMde;
        yield return PercentualAplicado;
        yield return PercentualMinimo;
        yield return Situacao;
    }
}
