using System.Globalization;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Pgv;

/// <summary>
/// Interpreta a chave de uma faixa de depreciação por idade da construção (fator da PGV). A chave é
/// definida por LEI MUNICIPAL e cadastrada como texto — nunca hardcoded. Formatos aceitos:
/// <list type="bullet">
/// <item><description><c>"min-max"</c> — intervalo fechado em anos, ambos inclusivos (ex.: <c>"6-10"</c>).</description></item>
/// <item><description><c>"min+"</c> — faixa aberta no topo, a partir de <c>min</c> (ex.: <c>"31+"</c>).</description></item>
/// <item><description><c>"n"</c> — idade única <c>n</c> (ex.: <c>"0"</c>), equivalente a <c>"n-n"</c>.</description></item>
/// </list>
/// A semântica de INTERVALO garante que uma idade (ex.: 7) case a faixa correta (ex.: "6-10") em vez
/// de exigir uma chave exata por idade — corrige a queda silenciosa em fator neutro 1 (IP-R4).
/// </summary>
public static class FaixaDepreciacao
{
    private const int IdadeMaxima = int.MaxValue;

    /// <summary>
    /// Tenta interpretar a chave de uma faixa de depreciação em seus limites inferior e superior
    /// (ambos inclusivos, em anos). Não lança: chaves malformadas retornam <c>false</c>.
    /// </summary>
    /// <param name="chave">Chave cadastrada da faixa (ex.: "6-10", "31+", "0").</param>
    /// <param name="minimo">Limite inferior da faixa (anos, inclusivo).</param>
    /// <param name="maximo">Limite superior da faixa (anos, inclusivo); <see cref="int.MaxValue"/> se aberta no topo.</param>
    /// <returns><c>true</c> se a chave foi interpretada; <c>false</c> se malformada.</returns>
    public static bool TentarInterpretar(string? chave, out int minimo, out int maximo)
    {
        minimo = 0;
        maximo = 0;

        if (string.IsNullOrWhiteSpace(chave))
        {
            return false;
        }

        var texto = chave.Trim();

        // Faixa aberta no topo: "min+".
        if (texto.EndsWith('+'))
        {
            var parteMin = texto[..^1].Trim();
            if (int.TryParse(parteMin, NumberStyles.Integer, CultureInfo.InvariantCulture, out var min) && min >= 0)
            {
                minimo = min;
                maximo = IdadeMaxima;
                return true;
            }

            return false;
        }

        // Intervalo fechado: "min-max".
        var hifen = texto.IndexOf('-', StringComparison.Ordinal);
        if (hifen > 0)
        {
            var parteMinimo = texto[..hifen].Trim();
            var parteMaximo = texto[(hifen + 1)..].Trim();
            if (int.TryParse(parteMinimo, NumberStyles.Integer, CultureInfo.InvariantCulture, out var min)
                && int.TryParse(parteMaximo, NumberStyles.Integer, CultureInfo.InvariantCulture, out var max)
                && min >= 0 && max >= min)
            {
                minimo = min;
                maximo = max;
                return true;
            }

            return false;
        }

        // Idade única: "n".
        if (int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unico) && unico >= 0)
        {
            minimo = unico;
            maximo = unico;
            return true;
        }

        return false;
    }
}
