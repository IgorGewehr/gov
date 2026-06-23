using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;

/// <summary>Situação da aferição do piso de remuneração dos profissionais da educação (FUNDEB 70%).</summary>
public enum SituacaoFundeb70
{
    /// <summary>Percentual pago a profissionais &gt;= piso: atingido.</summary>
    Atingido = 1,

    /// <summary>Percentual pago a profissionais &lt; piso: não atingido.</summary>
    NaoAtingido = 2,
}

/// <summary>
/// <b>E-2 — Indicador de aplicação do FUNDEB no piso de remuneração dos profissionais da educação.</b>
/// Resultado imutável e auto-calculado: a <b>receita FUNDEB do exercício</b> (cota-parte +
/// complementações recebidas), a <b>remuneração paga aos profissionais da educação básica</b> (folha do
/// magistério — vem do RH via Contracts, ou entra como total parametrizável até o cruzamento), o
/// <b>percentual aplicado</b>, o <b>piso vigente</b> (default legal <b>70%</b> — EC 108/2020, que elevou
/// o antigo piso de 60% do magistério para 70% dos profissionais da educação, parametrizável) e a
/// <b>situação</b>. Reprodutível (sem relógio): só depende das bases informadas. Espelha o
/// <c>IndicadorAsps</c> da Saúde.
/// <para>
/// // TODO(validar-oficial): o <b>rol exato de "profissionais da educação básica"</b> que entra no
/// numerador dos 70% (divergência interpretativa TCE/CNM) é parametrizável e segue a Lei 14.113/2020 +
/// instrumento do CACS-FUNDEB vigente.
/// </para>
/// </summary>
public sealed class IndicadorAplicacaoFundeb : ValueObject
{
    private IndicadorAplicacaoFundeb(
        decimal receitaFundeb,
        decimal remuneracaoProfissionais,
        decimal percentualAplicado,
        decimal pisoMinimo,
        SituacaoFundeb70 situacao)
    {
        ReceitaFundeb = receitaFundeb;
        RemuneracaoProfissionais = remuneracaoProfissionais;
        PercentualAplicado = percentualAplicado;
        PisoMinimo = pisoMinimo;
        Situacao = situacao;
    }

    /// <summary>Receita FUNDEB do exercício (cota-parte + complementações recebidas).</summary>
    public decimal ReceitaFundeb { get; }

    /// <summary>Remuneração paga aos profissionais da educação básica (folha do magistério — RH/Contracts).</summary>
    public decimal RemuneracaoProfissionais { get; }

    /// <summary>Percentual aplicado = RemuneracaoProfissionais / ReceitaFundeb (0..1, 4 casas), 0 quando base nula.</summary>
    public decimal PercentualAplicado { get; }

    /// <summary>Piso mínimo vigente (default 0,70 — EC 108/2020). Parametrizável.</summary>
    public decimal PisoMinimo { get; }

    /// <summary>Situação da aferição do piso de 70%.</summary>
    public SituacaoFundeb70 Situacao { get; }

    /// <summary>Indica se o piso de 70% foi atingido.</summary>
    public bool Atingido => Situacao == SituacaoFundeb70.Atingido;

    /// <summary>Margem em pontos do percentual aplicado sobre o piso (negativa quando insuficiente).</summary>
    public decimal MargemPontos => PercentualAplicado - PisoMinimo;

    /// <summary>
    /// Apura o indicador a partir da receita FUNDEB, da remuneração paga aos profissionais e do piso
    /// vigente. O percentual aplicado é arredondado a 4 casas (precisão de aferição); a situação compara o
    /// aplicado já arredondado com o piso (evita falso "não atingido" por dízima).
    /// </summary>
    /// <param name="receitaFundeb">Receita FUNDEB do exercício (&gt;= 0).</param>
    /// <param name="remuneracaoProfissionais">Remuneração paga aos profissionais da educação (&gt;= 0).</param>
    /// <param name="pisoMinimo">Piso mínimo vigente (0..1; default legal 70%).</param>
    /// <returns>Indicador apurado.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se algum valor for negativo ou o piso fora de 0..1.</exception>
    public static IndicadorAplicacaoFundeb Apurar(decimal receitaFundeb, decimal remuneracaoProfissionais, decimal pisoMinimo)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(receitaFundeb);
        ArgumentOutOfRangeException.ThrowIfNegative(remuneracaoProfissionais);
        ArgumentOutOfRangeException.ThrowIfNegative(pisoMinimo);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pisoMinimo, 1m);

        var percentualAplicado = receitaFundeb == 0m
            ? 0m
            : decimal.Round(remuneracaoProfissionais / receitaFundeb, 4, MidpointRounding.AwayFromZero);

        var situacao = percentualAplicado >= pisoMinimo
            ? SituacaoFundeb70.Atingido
            : SituacaoFundeb70.NaoAtingido;

        return new IndicadorAplicacaoFundeb(receitaFundeb, remuneracaoProfissionais, percentualAplicado, pisoMinimo, situacao);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ReceitaFundeb;
        yield return RemuneracaoProfissionais;
        yield return PercentualAplicado;
        yield return PisoMinimo;
        yield return Situacao;
    }
}
