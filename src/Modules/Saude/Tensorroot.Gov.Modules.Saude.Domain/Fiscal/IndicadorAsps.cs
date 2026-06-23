using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Fiscal;

/// <summary>Situação da aferição do mínimo de aplicação em ASPS.</summary>
public enum SituacaoAsps
{
    /// <summary>Percentual aplicado &gt;= mínimo: atingido.</summary>
    Atingido = 1,

    /// <summary>Percentual aplicado &lt; mínimo: não atingido.</summary>
    NaoAtingido = 2,
}

/// <summary>
/// <b>S-1 — Indicador ASPS (no espírito do SIOPS).</b> Resultado imutável e auto-calculado da apuração
/// do mínimo de Saúde para um exercício: a <b>receita-base</b> (impostos + transferências
/// constitucionais), o <b>aplicado em ASPS</b> (só a despesa de Saúde classificada como computável),
/// o <b>percentual aplicado</b>, o <b>percentual mínimo vigente</b> (default legal 15% — LC 141/2012,
/// parametrizável) e a <b>situação</b>. Reprodutível (sem relógio): só depende das bases informadas.
/// </summary>
public sealed class IndicadorAsps : ValueObject
{
    private IndicadorAsps(
        decimal receitaBase,
        decimal aplicadoAsps,
        decimal percentualAplicado,
        decimal percentualMinimo,
        SituacaoAsps situacao)
    {
        ReceitaBase = receitaBase;
        AplicadoAsps = aplicadoAsps;
        PercentualAplicado = percentualAplicado;
        PercentualMinimo = percentualMinimo;
        Situacao = situacao;
    }

    /// <summary>Receita-base do mínimo (impostos + transferências constitucionais).</summary>
    public decimal ReceitaBase { get; }

    /// <summary>Valor aplicado em ASPS (despesa de Saúde computável, art. 3º).</summary>
    public decimal AplicadoAsps { get; }

    /// <summary>Percentual aplicado = AplicadoAsps / ReceitaBase (0..1, 4 casas), 0 quando base nula.</summary>
    public decimal PercentualAplicado { get; }

    /// <summary>Percentual mínimo vigente (default 0,15; Lei Orgânica pode fixar maior). Parametrizável.</summary>
    public decimal PercentualMinimo { get; }

    /// <summary>Situação da aferição.</summary>
    public SituacaoAsps Situacao { get; }

    /// <summary>Indica se o mínimo de Saúde foi atingido.</summary>
    public bool Atingido => Situacao == SituacaoAsps.Atingido;

    /// <summary>Margem em pontos do percentual aplicado sobre o mínimo (negativa quando insuficiente).</summary>
    public decimal MargemPontos => PercentualAplicado - PercentualMinimo;

    /// <summary>
    /// Apura o indicador a partir da receita-base, do aplicado em ASPS e do percentual mínimo vigente. O
    /// percentual aplicado é arredondado a 4 casas (precisão de aferição); a situação compara o aplicado
    /// já arredondado com o mínimo (evita falso "não atingido" por dízima).
    /// </summary>
    /// <param name="receitaBase">Receita-base (&gt;= 0).</param>
    /// <param name="aplicadoAsps">Valor aplicado em ASPS (&gt;= 0).</param>
    /// <param name="percentualMinimo">Percentual mínimo vigente (0..1).</param>
    /// <returns>Indicador apurado.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se algum valor for negativo ou o percentual fora de 0..1.</exception>
    public static IndicadorAsps Apurar(decimal receitaBase, decimal aplicadoAsps, decimal percentualMinimo)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(receitaBase);
        ArgumentOutOfRangeException.ThrowIfNegative(aplicadoAsps);
        ArgumentOutOfRangeException.ThrowIfNegative(percentualMinimo);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentualMinimo, 1m);

        var percentualAplicado = receitaBase == 0m
            ? 0m
            : decimal.Round(aplicadoAsps / receitaBase, 4, MidpointRounding.AwayFromZero);

        var situacao = percentualAplicado >= percentualMinimo
            ? SituacaoAsps.Atingido
            : SituacaoAsps.NaoAtingido;

        return new IndicadorAsps(receitaBase, aplicadoAsps, percentualAplicado, percentualMinimo, situacao);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ReceitaBase;
        yield return AplicadoAsps;
        yield return PercentualAplicado;
        yield return PercentualMinimo;
        yield return Situacao;
    }
}
