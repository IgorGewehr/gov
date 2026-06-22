namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

/// <summary>
/// CRC-16/CCITT usado na validacao de integridade das linhas/arquivo do AFD (Portaria MTP 671/2021).
/// // TODO(validar-oficial): confirmar a VARIANTE exata exigida pela 671 (polinomio 0x1021; valor
/// inicial 0xFFFF [CCITT-FALSE] vs 0x0000 [XMODEM]; refletido ou nao). Adotamos CCITT-FALSE
/// (init 0xFFFF, sem reflexao, sem xorout) como conceito-base ate obter o Anexo oficial.
/// </summary>
public static class Crc16Ccitt
{
    private const ushort Polinomio = 0x1021;
    private const ushort ValorInicial = 0xFFFF;

    /// <summary>Calcula o CRC-16/CCITT-FALSE dos bytes informados.</summary>
    /// <param name="dados">Bytes de entrada (ja na codificacao do arquivo, p.ex. ISO-8859-1).</param>
    /// <returns>CRC de 16 bits.</returns>
    public static ushort Calcular(ReadOnlySpan<byte> dados)
    {
        ushort crc = ValorInicial;
        foreach (var b in dados)
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0
                    ? (ushort)((crc << 1) ^ Polinomio)
                    : (ushort)(crc << 1);
            }
        }

        return crc;
    }

    /// <summary>Calcula o CRC-16 e devolve em hexadecimal de 4 digitos (maiusculas).</summary>
    /// <param name="dados">Bytes de entrada.</param>
    /// <returns>CRC como 4 caracteres hex.</returns>
    public static string CalcularHex(ReadOnlySpan<byte> dados)
        => Calcular(dados).ToString("X4", System.Globalization.CultureInfo.InvariantCulture);
}
