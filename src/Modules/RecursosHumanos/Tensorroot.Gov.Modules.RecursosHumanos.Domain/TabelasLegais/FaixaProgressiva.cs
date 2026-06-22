using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

/// <summary>
/// Faixa de uma tabela progressiva de contribuicao (INSS/RPPS): aplica-se a parcela da base entre
/// <see cref="LimiteInferior"/> (exclusivo) e <see cref="LimiteSuperior"/> (inclusivo) a aliquota
/// informada. Objeto de Valor; nasce valido. Os numeros sao SEMPRE dado parametrizado, nunca
/// hardcoded no motor (CLAUDE.md S7/S16).
/// </summary>
public sealed class FaixaProgressiva : ValueObject
{
    private FaixaProgressiva(decimal limiteInferior, decimal limiteSuperior, decimal aliquota)
    {
        LimiteInferior = limiteInferior;
        LimiteSuperior = limiteSuperior;
        Aliquota = aliquota;
    }

    /// <summary>Limite inferior da faixa (exclusivo); a primeira faixa parte de zero.</summary>
    public decimal LimiteInferior { get; }

    /// <summary>Limite superior da faixa (inclusivo). A ultima faixa coincide com o teto.</summary>
    public decimal LimiteSuperior { get; }

    /// <summary>Aliquota da faixa em fracao decimal (ex.: 0,075 para 7,5%).</summary>
    public decimal Aliquota { get; }

    /// <summary>Cria uma faixa progressiva valida.</summary>
    /// <param name="limiteInferior">Limite inferior (exclusivo), maior ou igual a zero.</param>
    /// <param name="limiteSuperior">Limite superior (inclusivo), maior que o inferior.</param>
    /// <param name="aliquota">Aliquota em fracao decimal (0 a 1).</param>
    /// <returns>Instancia valida de <see cref="FaixaProgressiva"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se os limites/aliquota forem invalidos.</exception>
    public static FaixaProgressiva De(decimal limiteInferior, decimal limiteSuperior, decimal aliquota)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(limiteInferior);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(limiteSuperior, limiteInferior);
        ArgumentOutOfRangeException.ThrowIfNegative(aliquota);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(aliquota, 1m);
        return new FaixaProgressiva(limiteInferior, limiteSuperior, aliquota);
    }

    /// <summary>
    /// Calcula a parcela de contribuicao desta faixa para a base informada (mecanica cumulativa:
    /// cada faixa incide apenas sobre a parcela da base contida no intervalo). NAO arredonda: o
    /// arredondamento e feito UMA vez sobre a soma das faixas (semantica oficial INSS/RPPS), para
    /// evitar erro de acumulo de arredondamento por faixa.
    /// </summary>
    /// <param name="base">Base de contribuicao.</param>
    /// <returns>Contribuicao bruta da faixa (zero se a base nao alcanca o intervalo).</returns>
    public decimal Contribuicao(decimal @base)
    {
        if (@base <= LimiteInferior)
        {
            return 0m;
        }

        var topo = @base < LimiteSuperior ? @base : LimiteSuperior;
        return (topo - LimiteInferior) * Aliquota;
    }

    /// <inheritdoc />
    public override string ToString()
        => string.Create(CultureInfo.InvariantCulture, $"({LimiteInferior:0.00};{LimiteSuperior:0.00}]@{Aliquota:0.0000}");

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LimiteInferior;
        yield return LimiteSuperior;
        yield return Aliquota;
    }
}
