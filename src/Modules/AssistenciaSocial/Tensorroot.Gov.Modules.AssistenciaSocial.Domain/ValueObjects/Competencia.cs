using System.Globalization;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

/// <summary>
/// Competencia (ano/mes) de referencia da concessao. A regra de elegibilidade aplicada
/// e a vigente na competencia (nunca hardcoded) — Beneficio I-1.
/// </summary>
/// <param name="Ano">Ano (>= 1900).</param>
/// <param name="Mes">Mes (1 a 12).</param>
public readonly record struct Competencia(int Ano, int Mes)
{
    /// <summary>Cria uma competencia validada.</summary>
    /// <param name="ano">Ano (>= 1900).</param>
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

    /// <summary>Indica se a competencia esta preenchida (ano e mes validos).</summary>
    /// <returns><c>true</c> quando ano >= 1900 e mes entre 1 e 12.</returns>
    public bool EhValida() => Ano >= 1900 && Mes is >= 1 and <= 12;

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Mes:00}/{Ano:0000}");
}
