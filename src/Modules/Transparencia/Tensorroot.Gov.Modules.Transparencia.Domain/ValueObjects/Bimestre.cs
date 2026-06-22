using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.ValueObjects;

/// <summary>Periodo bimestral (ano e numero 1..6) do RREO.</summary>
public sealed class Bimestre : ValueObject
{
    private Bimestre(int ano, int numero)
    {
        Ano = ano;
        Numero = numero;
    }

    /// <summary>Ano (&gt;= 1900).</summary>
    public int Ano { get; }

    /// <summary>Numero do bimestre (1 a 6).</summary>
    public int Numero { get; }

    /// <summary>Cria um bimestre valido.</summary>
    /// <param name="ano">Ano (&gt;= 1900).</param>
    /// <param name="numero">Numero do bimestre (1 a 6).</param>
    /// <returns>Instancia de <see cref="Bimestre"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Quando ano ou numero estao fora do intervalo.</exception>
    public static Bimestre De(int ano, int numero)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ano, 1900);
        ArgumentOutOfRangeException.ThrowIfLessThan(numero, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(numero, 6);
        return new Bimestre(ano, numero);
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Ano:0000}-B{Numero:00}");

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Ano;
        yield return Numero;
    }
}
