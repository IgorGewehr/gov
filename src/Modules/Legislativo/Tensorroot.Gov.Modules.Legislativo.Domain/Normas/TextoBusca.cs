using System.Globalization;
using System.Text;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Normas;

/// <summary>
/// Normalizador de texto para busca textual de normas (BUG-3): converte para minusculas (invariante)
/// e remove diacriticos (acentos/cedilha), de modo que "acacias" case "Acacias"/"Acácias" e
/// "sao joao" case "São João" — sem depender da collation do banco. Aplicado tanto na coluna-sombra
/// indexada (na gravacao) quanto no termo consultado (antes do LIKE), garantindo simetria.
/// </summary>
public static class TextoBusca
{
    /// <summary>
    /// Normaliza o texto para comparacao de busca: lowercase invariante + remocao de diacriticos.
    /// </summary>
    /// <param name="texto">Texto de origem (ementa crua ou termo de busca).</param>
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
            // Descarta as marcas de combinacao (acentos) resultantes da decomposicao FormD.
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
}
