namespace Tensorroot.Gov.SharedKernel.ValueObjects;

/// <summary>Utilitários internos para extração e verificação de dígitos de documentos.</summary>
internal static class DigitoUtil
{
    /// <summary>Extrai apenas os dígitos ASCII de uma entrada, sem alocar em heap para entradas curtas.</summary>
    public static string ExtractDigits(string value)
    {
        Span<char> buffer = value.Length <= 32 ? stackalloc char[value.Length] : new char[value.Length];
        var count = 0;
        foreach (var c in value)
        {
            if (char.IsAsciiDigit(c))
            {
                buffer[count++] = c;
            }
        }

        return new string(buffer[..count]);
    }

    /// <summary>Indica se todos os caracteres da sequência são idênticos (ex.: "11111111111").</summary>
    public static bool AllSameDigit(string value)
    {
        for (var i = 1; i < value.Length; i++)
        {
            if (value[i] != value[0])
            {
                return false;
            }
        }

        return true;
    }
}
