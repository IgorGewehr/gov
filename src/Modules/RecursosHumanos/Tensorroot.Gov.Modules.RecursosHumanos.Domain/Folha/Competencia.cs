using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

/// <summary>
/// Competencia (mes/ano) de referencia de uma folha de pagamento, no formato <c>AAAA-MM</c>.
/// Unica por tenant (uma folha por competencia).
/// </summary>
public sealed class Competencia : ValueObject
{
    private Competencia(int ano, int mes)
    {
        Ano = ano;
        Mes = mes;
    }

    /// <summary>Ano da competencia (2000 a 2100).</summary>
    public int Ano { get; }

    /// <summary>Mes da competencia (1 a 12).</summary>
    public int Mes { get; }

    /// <summary>Cria uma competencia valida.</summary>
    /// <param name="ano">Ano (2000 a 2100).</param>
    /// <param name="mes">Mes (1 a 12).</param>
    /// <returns>Instancia de <see cref="Competencia"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Quando ano ou mes estao fora do intervalo.</exception>
    public static Competencia De(int ano, int mes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ano, 2000);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(ano, 2100);
        ArgumentOutOfRangeException.ThrowIfLessThan(mes, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mes, 12);
        return new Competencia(ano, mes);
    }

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Ano:0000}-{Mes:00}");

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Ano;
        yield return Mes;
    }
}
