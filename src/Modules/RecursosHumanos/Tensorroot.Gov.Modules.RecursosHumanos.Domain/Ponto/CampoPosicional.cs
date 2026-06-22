using System.Globalization;
using System.Text;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

/// <summary>
/// Utilitarios de formatacao de campos de largura fixa para os arquivos posicionais do ponto
/// (AFD/AEJ — Portaria MTP 671/2021): numericos alinhados a direita com zeros, alfanumericos a
/// esquerda com espacos, datas/horas no padrao do leiaute. Mantem a montagem fiel ao CONCEITO
/// posicional; // TODO(validar-oficial) larguras/ordem exatas por Anexo da 671.
/// </summary>
internal static class CampoPosicional
{
    /// <summary>Codificacao oficial dos arquivos AFD/AEJ (texto ASCII estendido ISO-8859-1).</summary>
    public static readonly Encoding CodificacaoArquivo = Encoding.Latin1;

    /// <summary>Terminador de linha exigido (CR+LF — caracteres 13 e 10).</summary>
    public const string FimDeLinha = "\r\n";

    /// <summary>Numerico alinhado a direita, preenchido com zeros, na largura informada.</summary>
    /// <param name="valor">Valor numerico (nao negativo).</param>
    /// <param name="largura">Largura do campo.</param>
    /// <returns>Texto zero-preenchido (truncado a esquerda se exceder).</returns>
    public static string Numero(long valor, int largura)
    {
        var texto = valor.ToString(CultureInfo.InvariantCulture).PadLeft(largura, '0');
        return texto.Length > largura ? texto[^largura..] : texto;
    }

    /// <summary>Alfanumerico alinhado a esquerda, preenchido com espacos, na largura informada.</summary>
    /// <param name="valor">Texto (nulo vira vazio).</param>
    /// <param name="largura">Largura do campo.</param>
    /// <returns>Texto preenchido/truncado na largura.</returns>
    public static string Texto(string? valor, int largura)
    {
        var t = (valor ?? string.Empty).Trim();
        return t.Length > largura ? t[..largura] : t.PadRight(largura, ' ');
    }

    /// <summary>Data no formato <c>ddMMyyyy</c> (8 posicoes) — padrao recorrente nos leiautes do MTP.</summary>
    /// <param name="data">Data a formatar.</param>
    /// <returns>Data como 8 digitos. // TODO(validar-oficial: ddMMyyyy vs yyyyMMdd por registro).</returns>
    public static string Data(DateOnly data)
        => data.ToString("ddMMyyyy", CultureInfo.InvariantCulture);

    /// <summary>Hora no formato <c>HHmm</c> (4 posicoes).</summary>
    /// <param name="hora">Instante a formatar (hora local).</param>
    /// <returns>Hora como 4 digitos.</returns>
    public static string Hora(DateTimeOffset hora)
        => hora.ToString("HHmm", CultureInfo.InvariantCulture);
}
