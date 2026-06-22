using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.ValueObjects;

/// <summary>Competencia mensal (ano e mes) da Matriz de Saldos Contabeis (MSC).</summary>
public sealed class Competencia : ValueObject
{
    private Competencia(int ano, int mes)
    {
        Ano = ano;
        Mes = mes;
    }

    /// <summary>Ano (&gt;= 1900).</summary>
    public int Ano { get; }

    /// <summary>Mes (1 a 12).</summary>
    public int Mes { get; }

    /// <summary>Cria uma competencia valida.</summary>
    /// <param name="ano">Ano (&gt;= 1900).</param>
    /// <param name="mes">Mes (1 a 12).</param>
    /// <returns>Instancia de <see cref="Competencia"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Quando ano ou mes estao fora do intervalo.</exception>
    public static Competencia De(int ano, int mes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ano, 1900);
        ArgumentOutOfRangeException.ThrowIfLessThan(mes, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mes, 12);
        return new Competencia(ano, mes);
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Mes:00}/{Ano:0000}");

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Ano;
        yield return Mes;
    }
}
