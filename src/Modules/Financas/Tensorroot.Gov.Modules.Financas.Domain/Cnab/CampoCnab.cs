using System.Globalization;
using System.Text;

namespace Tensorroot.Gov.Modules.Financas.Domain.Cnab;

/// <summary>
/// Utilitários de formatação de campos do layout posicional CNAB240 (FEBRABAN): numéricos alinhados à
/// direita com zeros à esquerda; alfanuméricos alinhados à esquerda com brancos à direita; remoção de
/// acentos/maiúsculas (padrão do arquivo bancário). Todos os campos têm tamanho fixo.
/// </summary>
public static class CampoCnab
{
    /// <summary>Formata um campo numérico (zeros à esquerda), truncando à direita se exceder.</summary>
    /// <param name="valor">Valor (apenas dígitos serão mantidos).</param>
    /// <param name="tamanho">Tamanho do campo.</param>
    /// <returns>Campo de tamanho fixo.</returns>
    public static string Numerico(string? valor, int tamanho)
    {
        var digitos = new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digitos.Length > tamanho)
        {
            digitos = digitos[^tamanho..];
        }

        return digitos.PadLeft(tamanho, '0');
    }

    /// <summary>Formata um inteiro como campo numérico.</summary>
    /// <param name="valor">Valor inteiro.</param>
    /// <param name="tamanho">Tamanho do campo.</param>
    /// <returns>Campo de tamanho fixo.</returns>
    public static string Numerico(long valor, int tamanho)
        => Numerico(valor.ToString(CultureInfo.InvariantCulture), tamanho);

    /// <summary>
    /// Formata um valor monetário como inteiro de centavos (sem separadores), zeros à esquerda.
    /// </summary>
    /// <param name="valor">Valor em reais.</param>
    /// <param name="tamanho">Tamanho do campo (inclui as 2 casas implícitas).</param>
    /// <returns>Campo de tamanho fixo.</returns>
    public static string Valor(decimal valor, int tamanho)
    {
        var centavos = (long)decimal.Round(valor * 100m, 0, MidpointRounding.AwayFromZero);
        return Numerico(centavos, tamanho);
    }

    /// <summary>Formata um campo alfanumérico (brancos à direita), em maiúsculas sem acento.</summary>
    /// <param name="valor">Valor.</param>
    /// <param name="tamanho">Tamanho do campo.</param>
    /// <returns>Campo de tamanho fixo.</returns>
    public static string Alfanumerico(string? valor, int tamanho)
    {
        var limpo = RemoverAcentos(valor ?? string.Empty).ToUpperInvariant();
        if (limpo.Length > tamanho)
        {
            limpo = limpo[..tamanho];
        }

        return limpo.PadRight(tamanho, ' ');
    }

    /// <summary>Formata uma data no padrão DDMMAAAA.</summary>
    /// <param name="data">Data.</param>
    /// <returns>Campo de 8 posições.</returns>
    public static string Data(DateOnly data)
        => data.ToString("ddMMyyyy", CultureInfo.InvariantCulture);

    /// <summary>Formata uma hora no padrão HHMMSS.</summary>
    /// <param name="hora">Hora.</param>
    /// <returns>Campo de 6 posições.</returns>
    public static string Hora(TimeOnly hora)
        => hora.ToString("HHmmss", CultureInfo.InvariantCulture);

    /// <summary>Espaços em branco.</summary>
    /// <param name="tamanho">Quantidade.</param>
    /// <returns>Brancos.</returns>
    public static string Brancos(int tamanho) => new(' ', tamanho);

    /// <summary>Zeros.</summary>
    /// <param name="tamanho">Quantidade.</param>
    /// <returns>Zeros.</returns>
    public static string Zeros(int tamanho) => new('0', tamanho);

    private static string RemoverAcentos(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizado.Length);
        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
