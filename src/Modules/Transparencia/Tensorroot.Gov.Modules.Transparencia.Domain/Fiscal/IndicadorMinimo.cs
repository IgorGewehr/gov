using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

/// <summary>Situação da aferição de um mínimo constitucional.</summary>
public enum SituacaoMinimo
{
    /// <summary>Percentual aplicado &gt;= percentual mínimo: atingido.</summary>
    Atingido = 1,

    /// <summary>Percentual aplicado &lt; percentual mínimo: não atingido.</summary>
    NaoAtingido = 2,
}

/// <summary>
/// <b>M7.0.3.</b> Indicador de um mínimo constitucional apurado para um setor/período: a
/// <b>receita-base</b> (impostos + transferências constitucionais), o valor <b>aplicado</b> (despesa
/// computável do setor), o <b>percentual aplicado</b>, o <b>percentual mínimo (limite)</b> vigente e a
/// <b>situação</b> (atingido / não atingido). Imutável e auto-calculado a partir das bases — sem
/// relógio, reprodutível.
/// </summary>
public sealed class IndicadorMinimo : ValueObject
{
    private IndicadorMinimo(
        SetorMinimo setor,
        decimal receitaBase,
        decimal aplicado,
        decimal percentualAplicado,
        decimal percentualMinimo,
        SituacaoMinimo situacao)
    {
        Setor = setor;
        ReceitaBase = receitaBase;
        Aplicado = aplicado;
        PercentualAplicado = percentualAplicado;
        PercentualMinimo = percentualMinimo;
        Situacao = situacao;
    }

    /// <summary>Setor apurado (Saúde/Educação).</summary>
    public SetorMinimo Setor { get; }

    /// <summary>Receita-base do mínimo (impostos + transferências constitucionais).</summary>
    public decimal ReceitaBase { get; }

    /// <summary>Valor aplicado no setor (despesa computável).</summary>
    public decimal Aplicado { get; }

    /// <summary>Percentual aplicado = Aplicado / ReceitaBase (0..1, 4 casas), 0 quando base nula.</summary>
    public decimal PercentualAplicado { get; }

    /// <summary>Percentual mínimo (limite) vigente, parametrizável (ex.: 0,15 Saúde / 0,25 Educação).</summary>
    public decimal PercentualMinimo { get; }

    /// <summary>Situação da aferição.</summary>
    public SituacaoMinimo Situacao { get; }

    /// <summary>Indica se o mínimo foi atingido.</summary>
    public bool Atingido => Situacao == SituacaoMinimo.Atingido;

    /// <summary>Diferença em pontos do percentual aplicado sobre o mínimo (negativa quando insuficiente).</summary>
    public decimal MargemPontos => PercentualAplicado - PercentualMinimo;

    /// <summary>
    /// Apura o indicador a partir da receita-base, do aplicado e do percentual mínimo vigente. O
    /// percentual aplicado é arredondado a 4 casas (precisão de aferição); a situação compara o aplicado
    /// já arredondado com o mínimo (evita falso "não atingido" por dízima).
    /// </summary>
    /// <param name="setor">Setor apurado.</param>
    /// <param name="receitaBase">Receita-base (&gt;= 0).</param>
    /// <param name="aplicado">Valor aplicado computável (&gt;= 0).</param>
    /// <param name="percentualMinimo">Percentual mínimo vigente (0..1).</param>
    /// <returns>Indicador apurado.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se algum valor for negativo ou o percentual fora de 0..1.</exception>
    public static IndicadorMinimo Apurar(SetorMinimo setor, decimal receitaBase, decimal aplicado, decimal percentualMinimo)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(receitaBase);
        ArgumentOutOfRangeException.ThrowIfNegative(aplicado);
        ArgumentOutOfRangeException.ThrowIfNegative(percentualMinimo);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentualMinimo, 1m);

        var percentualAplicado = receitaBase == 0m
            ? 0m
            : decimal.Round(aplicado / receitaBase, 4, MidpointRounding.AwayFromZero);

        var situacao = percentualAplicado >= percentualMinimo
            ? SituacaoMinimo.Atingido
            : SituacaoMinimo.NaoAtingido;

        return new IndicadorMinimo(setor, receitaBase, aplicado, percentualAplicado, percentualMinimo, situacao);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Setor;
        yield return ReceitaBase;
        yield return Aplicado;
        yield return PercentualAplicado;
        yield return PercentualMinimo;
        yield return Situacao;
    }
}
