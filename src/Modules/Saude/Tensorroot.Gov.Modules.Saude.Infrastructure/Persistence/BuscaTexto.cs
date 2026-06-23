using System.Globalization;
using System.Text;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence;

/// <summary>
/// Normalizador de texto para a busca de pacientes por nome (navegabilidade — Onda 0): lowercase
/// invariante + remocao de diacriticos, de modo que "joao" case "João" e "jose" case "José" sem
/// depender da collation do banco. Aplicado tanto na coluna-sombra indexada <c>NomeBusca</c> (na
/// gravacao) quanto no termo consultado (antes do LIKE), garantindo simetria. Espelha o padrao do
/// modulo Legislativo (TextoBusca/EmentaBusca).
/// </summary>
internal static class BuscaTexto
{
    /// <summary>Normaliza o texto para comparacao de busca: lowercase invariante + remocao de diacriticos.</summary>
    /// <param name="texto">Texto de origem (nome cru ou termo de busca).</param>
    /// <returns>Texto normalizado (vazio se a entrada for nula/vazia).</returns>
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return string.Empty;
        }

        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var construtor = new StringBuilder(decomposto.Length);

        foreach (var caractere in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            {
                construtor.Append(caractere);
            }
        }

        return construtor
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();
    }

    /// <summary>Monta o padrao LIKE "%termo%" do termo normalizado, com curingas escapados (literais).</summary>
    /// <param name="termoNormalizado">Termo ja normalizado por <see cref="Normalizar"/>.</param>
    /// <returns>Padrao pronto para <c>EF.Functions.Like(..., padrao, "\\")</c>.</returns>
    public static string MontarPadraoContains(string termoNormalizado)
        => "%" + Escapar(termoNormalizado) + "%";

    private static string Escapar(string termo)
        => termo.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    /// <summary>Extrai apenas os digitos do termo (para casar com a coluna-sombra de CPF/CNS).</summary>
    /// <param name="texto">Termo livre informado pelo usuario.</param>
    /// <returns>Somente os digitos do termo (vazio se nao houver).</returns>
    public static string SomenteDigitos(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return string.Empty;
        }

        var construtor = new StringBuilder(texto.Length);
        foreach (var caractere in texto)
        {
            if (char.IsAsciiDigit(caractere))
            {
                construtor.Append(caractere);
            }
        }

        return construtor.ToString();
    }
}
