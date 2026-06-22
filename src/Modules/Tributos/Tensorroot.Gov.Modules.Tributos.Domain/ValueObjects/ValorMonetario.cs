using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

/// <summary>Valor monetário em Reais (BRL), não-negativo, arredondado a 2 casas, com aritmética segura.</summary>
public sealed class ValorMonetario : ValueObject
{
    private ValorMonetario(decimal valor) => Valor = valor;

    /// <summary>Valor zero.</summary>
    public static ValorMonetario Zero { get; } = new(0m);

    /// <summary>Montante em Reais (2 casas decimais).</summary>
    public decimal Valor { get; }

    /// <summary>Cria um valor monetário não-negativo.</summary>
    /// <param name="valor">Montante (maior ou igual a zero).</param>
    /// <returns>Instância de <see cref="ValorMonetario"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor for negativo.</exception>
    public static ValorMonetario De(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        return new ValorMonetario(decimal.Round(valor, 2, MidpointRounding.AwayFromZero));
    }

    /// <summary>Soma este valor a outro.</summary>
    /// <param name="outro">Parcela a somar.</param>
    /// <returns>Novo valor com a soma.</returns>
    public ValorMonetario Somar(ValorMonetario outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        return new ValorMonetario(Valor + outro.Valor);
    }

    /// <summary>Calcula um acréscimo percentual (ex.: multa/juros) sobre este valor.</summary>
    /// <param name="percentual">Percentual não-negativo (ex.: 2 para 2%).</param>
    /// <returns>O valor do acréscimo.</returns>
    public ValorMonetario AplicarPercentual(decimal percentual)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(percentual);
        return new ValorMonetario(decimal.Round(Valor * percentual / 100m, 2, MidpointRounding.AwayFromZero));
    }

    /// <inheritdoc />
    public override string ToString() => Valor.ToString("0.00", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
