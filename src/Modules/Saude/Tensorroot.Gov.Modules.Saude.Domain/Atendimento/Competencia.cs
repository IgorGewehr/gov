using System.Globalization;

namespace Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

/// <summary>
/// Competencia (mes de referencia, AAAA-MM) da producao para o SISAB (Portaria 1.412/2013).
/// </summary>
public readonly record struct Competencia
{
    private Competencia(int ano, int mes)
    {
        Ano = ano;
        Mes = mes;
    }

    /// <summary>Ano da competencia.</summary>
    public int Ano { get; }

    /// <summary>Mes da competencia (1 a 12).</summary>
    public int Mes { get; }

    /// <summary>Cria uma competencia validada.</summary>
    /// <param name="ano">Ano (entre 1900 e 9999).</param>
    /// <param name="mes">Mes (1 a 12).</param>
    /// <returns>Instancia de <see cref="Competencia"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se ano ou mes forem invalidos.</exception>
    public static Competencia De(int ano, int mes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ano, 1900);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(ano, 9999);
        ArgumentOutOfRangeException.ThrowIfLessThan(mes, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mes, 12);
        return new Competencia(ano, mes);
    }

    /// <summary>Deriva a competencia a partir de uma data/hora (ano e mes da ocorrencia).</summary>
    /// <param name="dataHora">Data/hora de referencia.</param>
    /// <returns>Competencia correspondente.</returns>
    public static Competencia De(DateTimeOffset dataHora) => new(dataHora.Year, dataHora.Month);

    /// <inheritdoc />
    public override string ToString() => $"{Ano:D4}-{Mes:D2}";

    /// <summary>Converte a competencia para o formato AAAA-MM (cultura invariante).</summary>
    /// <returns>Texto AAAA-MM.</returns>
    public string ToCodigo() => string.Create(CultureInfo.InvariantCulture, $"{Ano:D4}-{Mes:D2}");
}
