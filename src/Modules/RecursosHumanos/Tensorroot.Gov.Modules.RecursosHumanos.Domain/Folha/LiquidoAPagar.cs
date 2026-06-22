using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

/// <summary>
/// Liquido a pagar de uma folha: total de proventos menos total de descontos. Nao-negativo
/// (apos abate-teto e demais descontos), 2 casas decimais (I-5).
/// </summary>
public sealed class LiquidoAPagar : ValueObject
{
    private LiquidoAPagar(decimal valor) => Valor = valor;

    /// <summary>Valor zero.</summary>
    public static LiquidoAPagar Zero { get; } = new(0m);

    /// <summary>Montante liquido em Reais (2 casas decimais).</summary>
    public decimal Valor { get; }

    /// <summary>Cria um liquido a pagar nao-negativo.</summary>
    /// <param name="valor">Montante (maior ou igual a zero).</param>
    /// <returns>Instancia de <see cref="LiquidoAPagar"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor for negativo (I-5 / B-10).</exception>
    public static LiquidoAPagar De(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        return new LiquidoAPagar(decimal.Round(valor, 2, MidpointRounding.AwayFromZero));
    }

    /// <inheritdoc />
    public override string ToString() => Valor.ToString("0.00", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
