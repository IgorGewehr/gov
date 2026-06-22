using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

/// <summary>
/// Faixa da tabela progressiva mensal do IRRF (modelo "aliquota x base menos parcela a deduzir"):
/// para a base na faixa, o imposto = base x <see cref="Aliquota"/> menos <see cref="ParcelaDeduzir"/>.
/// Objeto de Valor; numeros parametrizados (Receita Federal), nunca hardcoded (CLAUDE.md S16).
/// </summary>
public sealed class FaixaIrrf : ValueObject
{
    private FaixaIrrf(decimal limiteSuperior, decimal aliquota, decimal parcelaDeduzir)
    {
        LimiteSuperior = limiteSuperior;
        Aliquota = aliquota;
        ParcelaDeduzir = parcelaDeduzir;
    }

    /// <summary>Limite superior da faixa (inclusivo). A ultima faixa usa <see cref="decimal.MaxValue"/>.</summary>
    public decimal LimiteSuperior { get; }

    /// <summary>Aliquota da faixa em fracao decimal (ex.: 0,275 para 27,5%). A faixa isenta tem aliquota zero.</summary>
    public decimal Aliquota { get; }

    /// <summary>Parcela a deduzir do imposto apurado nesta faixa.</summary>
    public decimal ParcelaDeduzir { get; }

    /// <summary>Cria uma faixa de IRRF valida.</summary>
    /// <param name="limiteSuperior">Limite superior (inclusivo); a ultima faixa = <see cref="decimal.MaxValue"/>.</param>
    /// <param name="aliquota">Aliquota em fracao decimal (0 a 1).</param>
    /// <param name="parcelaDeduzir">Parcela a deduzir (maior ou igual a zero).</param>
    /// <returns>Instancia valida de <see cref="FaixaIrrf"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se os valores forem invalidos.</exception>
    public static FaixaIrrf De(decimal limiteSuperior, decimal aliquota, decimal parcelaDeduzir)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limiteSuperior);
        ArgumentOutOfRangeException.ThrowIfNegative(aliquota);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(aliquota, 1m);
        ArgumentOutOfRangeException.ThrowIfNegative(parcelaDeduzir);
        return new FaixaIrrf(limiteSuperior, aliquota, parcelaDeduzir);
    }

    /// <summary>Indica se a base se enquadra nesta faixa (ate o limite superior, inclusivo).</summary>
    /// <param name="base">Base de calculo do IRRF.</param>
    /// <returns><c>true</c> se a base for menor ou igual ao limite superior.</returns>
    public bool Enquadra(decimal @base) => @base <= LimiteSuperior;

    /// <summary>Apura o imposto desta faixa para a base informada (nunca negativo).</summary>
    /// <param name="base">Base de calculo do IRRF.</param>
    /// <returns>Imposto apurado (2 casas), nao-negativo.</returns>
    public decimal Imposto(decimal @base)
    {
        var imposto = (@base * Aliquota) - ParcelaDeduzir;
        return imposto < 0m ? 0m : decimal.Round(imposto, 2, MidpointRounding.AwayFromZero);
    }

    /// <inheritdoc />
    public override string ToString()
        => string.Create(CultureInfo.InvariantCulture, $"<={LimiteSuperior:0.00}@{Aliquota:0.0000}-{ParcelaDeduzir:0.00}");

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LimiteSuperior;
        yield return Aliquota;
        yield return ParcelaDeduzir;
    }
}
