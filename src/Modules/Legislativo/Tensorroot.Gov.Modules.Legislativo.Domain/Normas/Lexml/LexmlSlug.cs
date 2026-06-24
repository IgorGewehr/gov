using System.Globalization;
using System.Text;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Normas.Lexml;

/// <summary>
/// Normalizador de componentes da URN LexML-BR. A convencao LexML usa identificadores em caixa-baixa,
/// sem diacriticos, com palavras separadas por ponto (ex.: "Maximiliano de Almeida" -> "maximiliano.de.almeida").
/// Centraliza a regra para que ente, autoridade e tipo de norma compartilhem a mesma grafia canonica.
/// </summary>
public static class LexmlSlug
{
    /// <summary>
    /// Converte um texto livre na grafia LexML: minusculo, sem acentos, separadores (espaco, traco,
    /// underscore, ponto) colapsados em UM ponto; descarta os demais caracteres.
    /// </summary>
    /// <param name="texto">Texto de entrada (pode ser nulo/vazio).</param>
    /// <returns>Slug LexML (pode ser vazio se a entrada nao tiver caracteres uteis).</returns>
    public static string Slugificar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var semAcento = RemoverDiacriticos(texto).ToLowerInvariant();
        var resultado = new StringBuilder(semAcento.Length);
        var pendentePonto = false;

        foreach (var caractere in semAcento)
        {
            if (caractere is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                if (pendentePonto && resultado.Length > 0)
                {
                    resultado.Append('.');
                }

                pendentePonto = false;
                resultado.Append(caractere);
            }
            else if (caractere is ' ' or '-' or '_' or '.')
            {
                // Marca a necessidade de um separador, mas so o emite antes do proximo caractere util
                // (evita ponto inicial/final e pontos duplicados).
                pendentePonto = true;
            }

            // Demais caracteres (pontuacao, simbolos) sao descartados.
        }

        return resultado.ToString();
    }

    private static string RemoverDiacriticos(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var construtor = new StringBuilder(decomposto.Length);

        foreach (var caractere in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            {
                construtor.Append(caractere);
            }
        }

        return construtor.ToString().Normalize(NormalizationForm.FormC);
    }
}
