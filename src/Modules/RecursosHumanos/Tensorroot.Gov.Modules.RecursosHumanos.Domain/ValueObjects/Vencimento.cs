using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ValueObjects;

/// <summary>
/// Remuneracao-base (vencimento) de um cargo publico, em Reais (BRL): valor positivo,
/// arredondado a 2 casas. Sujeito ao teto remuneratorio (CF art. 37, XI), cujo abate-teto
/// e aplicado no calculo da folha (modulo FolhaDePagamento).
/// </summary>
public sealed class Vencimento : ValueObject
{
    private Vencimento(decimal valor) => Valor = valor;

    /// <summary>Montante do vencimento em Reais (2 casas decimais).</summary>
    public decimal Valor { get; }

    /// <summary>Cria um vencimento positivo.</summary>
    /// <param name="valor">Montante (maior que zero).</param>
    /// <returns>Instancia de <see cref="Vencimento"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor for menor ou igual a zero.</exception>
    public static Vencimento De(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valor);
        return new Vencimento(decimal.Round(valor, 2, MidpointRounding.AwayFromZero));
    }

    /// <inheritdoc />
    public override string ToString() => Valor.ToString("0.00", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
