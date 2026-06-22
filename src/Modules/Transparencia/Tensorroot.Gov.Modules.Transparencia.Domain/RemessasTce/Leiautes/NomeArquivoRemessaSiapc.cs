using System.Globalization;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

/// <summary>
/// Compõe o NOME do ZIP da remessa SIAPC/PAD conforme a convenção estruturada do TCE-RS.
/// </summary>
/// <remarks>
/// <para><b>Convenção do nome do ZIP</b> (verificação e-validador §4.3 do M4-DESIGN):</para>
/// <code>
///   CNPJ(14) . DataIni(ddmmaaaa) . DataFim(ddmmaaaa) . DataGer(ddmmaaaa) . Tipo(1) . CodRemessa(12) . zip
/// </code>
/// <para>
/// Exemplo: <c>99999999000199.01012007.31052007.15062007.P.000000000010.zip</c>.
/// <list type="bullet">
///   <item><c>CNPJ</c>: 14 dígitos do ente, sem máscara.</item>
///   <item><c>DataIni/DataFim</c>: início e fim do período de competência (ddmmaaaa).</item>
///   <item><c>DataGer</c>: data de geração do pacote (ddmmaaaa).</item>
///   <item><c>Tipo</c>: 1 letra do Setor de Governo — P=Prefeitura, C=Câmara, A/F/E/S/O.</item>
///   <item><c>CodRemessa</c>: Código da Remessa, Numérico 12, gerado pelo PRÓPRIO ENTE.</item>
/// </list>
/// </para>
/// <para>
/// CONFIANÇA: ALTA (verificação 4.3), mas a re-confirmar no MT 2026 —
/// <c>// TODO(validar-leiaute-MT-2026)</c> (ordem/separador exatos do exercício 2026).
/// </para>
/// </remarks>
public static class NomeArquivoRemessaSiapc
{
    private const string Separador = ".";
    private const string Extensao = "zip";
    private const int TamanhoCnpj = 14;
    private const int TamanhoCodRemessa = 12;

    /// <summary>Monta o nome do ZIP da remessa.</summary>
    /// <param name="cnpj">CNPJ do ente (14 dígitos, sem máscara).</param>
    /// <param name="dataInicio">Início do período de competência.</param>
    /// <param name="dataFim">Fim do período de competência.</param>
    /// <param name="dataGeracao">Data de geração do pacote.</param>
    /// <param name="tipoSetorGoverno">Tipo de Setor de Governo (1 letra).</param>
    /// <param name="codigoRemessa">Código da Remessa (numérico, até 12 dígitos), gerado pelo ente.</param>
    /// <returns>Nome do arquivo ZIP estruturado.</returns>
    /// <exception cref="ArgumentException">Se o CNPJ não tiver 14 dígitos ou o tipo não for letra.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o código da remessa for negativo ou exceder 12 dígitos.</exception>
    public static string Compor(
        string cnpj,
        DateOnly dataInicio,
        DateOnly dataFim,
        DateOnly dataGeracao,
        char tipoSetorGoverno,
        long codigoRemessa)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cnpj);

        var digitos = new string([.. cnpj.Where(char.IsDigit)]);
        if (digitos.Length != TamanhoCnpj)
        {
            throw new ArgumentException("CNPJ deve conter 14 dígitos.", nameof(cnpj));
        }

        if (!char.IsLetter(tipoSetorGoverno))
        {
            throw new ArgumentException("Tipo de Setor de Governo deve ser uma letra.", nameof(tipoSetorGoverno));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(codigoRemessa);
        var codigo = codigoRemessa.ToString(CultureInfo.InvariantCulture).PadLeft(TamanhoCodRemessa, '0');
        if (codigo.Length > TamanhoCodRemessa)
        {
            throw new ArgumentOutOfRangeException(
                nameof(codigoRemessa), codigoRemessa, "Código da remessa excede 12 dígitos.");
        }

        return string.Join(
            Separador,
            digitos,
            Data(dataInicio),
            Data(dataFim),
            Data(dataGeracao),
            char.ToUpperInvariant(tipoSetorGoverno).ToString(),
            codigo + Separador + Extensao);
    }

    private static string Data(DateOnly data)
        => string.Create(CultureInfo.InvariantCulture, $"{data.Day:00}{data.Month:00}{data.Year:0000}");
}
