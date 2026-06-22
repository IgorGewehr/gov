using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

/// <summary>Base de incidencia (valor) usada no calculo de uma rubrica da folha. Nao-negativa, 2 casas.</summary>
public sealed class BaseCalculo : ValueObject
{
    private BaseCalculo(decimal valor) => Valor = valor;

    /// <summary>Valor zero.</summary>
    public static BaseCalculo Zero { get; } = new(0m);

    /// <summary>Valor de incidencia em Reais (2 casas decimais).</summary>
    public decimal Valor { get; }

    /// <summary>Cria uma base de calculo nao-negativa.</summary>
    /// <param name="valor">Montante (maior ou igual a zero).</param>
    /// <returns>Instancia de <see cref="BaseCalculo"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor for negativo.</exception>
    public static BaseCalculo De(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        return new BaseCalculo(decimal.Round(valor, 2, MidpointRounding.AwayFromZero));
    }

    /// <inheritdoc />
    public override string ToString() => Valor.ToString("0.00", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
