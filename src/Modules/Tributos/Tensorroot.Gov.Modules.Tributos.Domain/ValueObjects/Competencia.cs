using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

/// <summary>Competência fiscal (ano e mês) de um lançamento tributário.</summary>
public sealed class Competencia : ValueObject
{
    private Competencia(int ano, int mes)
    {
        Ano = ano;
        Mes = mes;
    }

    /// <summary>Ano (>= 1900).</summary>
    public int Ano { get; }

    /// <summary>Mês (1 a 12).</summary>
    public int Mes { get; }

    /// <summary>Cria uma competência válida.</summary>
    /// <param name="ano">Ano (>= 1900).</param>
    /// <param name="mes">Mês (1 a 12).</param>
    /// <returns>Instância de <see cref="Competencia"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Quando ano ou mês estão fora do intervalo.</exception>
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
