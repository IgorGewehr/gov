using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.ValueObjects;

/// <summary>Periodo quadrimestral (ano e numero 1..3) do RGF.</summary>
public sealed class Quadrimestre : ValueObject
{
    private Quadrimestre(int ano, int numero)
    {
        Ano = ano;
        Numero = numero;
    }

    /// <summary>Ano (&gt;= 1900).</summary>
    public int Ano { get; }

    /// <summary>Numero do quadrimestre (1 a 3).</summary>
    public int Numero { get; }

    /// <summary>Cria um quadrimestre valido.</summary>
    /// <param name="ano">Ano (&gt;= 1900).</param>
    /// <param name="numero">Numero do quadrimestre (1 a 3).</param>
    /// <returns>Instancia de <see cref="Quadrimestre"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Quando ano ou numero estao fora do intervalo.</exception>
    public static Quadrimestre De(int ano, int numero)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ano, 1900);
        ArgumentOutOfRangeException.ThrowIfLessThan(numero, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(numero, 3);
        return new Quadrimestre(ano, numero);
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Ano:0000}-Q{Numero:00}");

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Ano;
        yield return Numero;
    }
}
