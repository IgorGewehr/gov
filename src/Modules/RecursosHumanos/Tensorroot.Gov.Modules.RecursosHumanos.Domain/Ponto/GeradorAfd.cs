using System.Globalization;
using System.Text;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

/// <summary>Cabecalho do AFD: identificacao do empregador (ente publico) e periodo do arquivo.</summary>
/// <param name="Cnpj">CNPJ do ente.</param>
/// <param name="RazaoSocial">Razao social/nome do ente.</param>
/// <param name="InicioPeriodo">Primeira data do periodo exportado.</param>
/// <param name="FimPeriodo">Ultima data do periodo exportado.</param>
/// <param name="DataHoraGeracao">Instante de geracao do arquivo.</param>
public sealed record CabecalhoAfd(
    Cnpj Cnpj,
    string RazaoSocial,
    DateOnly InicioPeriodo,
    DateOnly FimPeriodo,
    DateTimeOffset DataHoraGeracao);

/// <summary>Linha de marcacao do AFD (registro bruto e cronologico).</summary>
/// <param name="Nsr">NSR da marcacao.</param>
/// <param name="Cpf">CPF do trabalhador.</param>
/// <param name="DataHora">Data/hora exata da batida.</param>
/// <param name="Origem">Origem (REP-P/REP-C/REP-A).</param>
public sealed record LinhaMarcacaoAfd(Nsr Nsr, Cpf Cpf, DateTimeOffset DataHora, TipoRep Origem);

/// <summary>
/// Gerador do AFD (Arquivo Fonte de Dados — Portaria MTP 671/2021): monta o arquivo posicional
/// ASCII (ISO-8859-1) com CABECALHO + REGISTROS DE MARCACAO (em ordem de NSR) + TRAILER, terminando
/// cada linha com CR+LF. A estrutura segue FIELMENTE o CONCEITO da 671 (tipo de registro na 1a
/// posicao, NSR, CPF, data/hora). // TODO(validar-oficial): POSICOES e LARGURAS exatas de cada campo
/// e a tabela de tipos de registro do AFD nao foram obtidas do Anexo oficial (texto integral DOU);
/// ajustar antes de uso fiscal. Servico de dominio PURO (sem I/O); assinatura CAdES e externa.
/// </summary>
public static class GeradorAfd
{
    // Larguras CONCEITUAIS dos campos. // TODO(validar-oficial): substituir pelas do Anexo do AFD.
    // internal: o ParserAfd (mesma assembly Domain) reusa estas larguras para ser FIEL ao gerador
    // (uma unica fonte de verdade do leiaute posicional, inverso exato).
    internal const int LarguraTipo = 1;
    internal const int LarguraNsr = Nsr.LarguraPadrao;
    internal const int LarguraCnpj = 14;
    internal const int LarguraCpf = 11;
    internal const int LarguraRazao = 150;
    internal const int LarguraContador = 9;
    internal const int LarguraData = 8;
    internal const int LarguraHora = 4;
    internal const int LarguraCrc = 4;

    /// <summary>
    /// Seleciona o tipo de registro de marcacao conforme a origem: REP-P -&gt; tipo 7; REP-C/REP-A -&gt;
    /// tipo 3 (leiaute 671). // TODO(validar-oficial): confirmar a tabela de tipos no Anexo oficial.
    /// </summary>
    /// <param name="origem">Origem da marcacao (tipo de REP).</param>
    /// <returns>Tipo de registro do AFD (3 ou 7).</returns>
    internal static TipoRegistroAfd TipoRegistroMarcacao(TipoRep origem)
        => origem == TipoRep.RepP ? TipoRegistroAfd.MarcacaoRepP : TipoRegistroAfd.Marcacao;

    /// <summary>Indica se o codigo de tipo lido e uma linha de marcacao (3 ou 7).</summary>
    /// <param name="tipo">Codigo de tipo (1a posicao do registro).</param>
    /// <returns><c>true</c> para marcacao de REP-C/A (3) ou REP-P (7).</returns>
    internal static bool EhMarcacao(int tipo)
        => tipo == (int)TipoRegistroAfd.Marcacao || tipo == (int)TipoRegistroAfd.MarcacaoRepP;

    /// <summary>
    /// Gera os bytes do AFD (ISO-8859-1). As marcacoes sao ordenadas por NSR (cronologia do REP).
    /// </summary>
    /// <param name="cabecalho">Identificacao do empregador e periodo.</param>
    /// <param name="marcacoes">Marcacoes do periodo (qualquer ordem; serao ordenadas por NSR).</param>
    /// <returns>Conteudo do AFD em bytes, pronto para persistencia/assinatura CAdES.</returns>
    /// <exception cref="ArgumentNullException">Se cabecalho ou marcacoes forem nulos.</exception>
    public static byte[] Gerar(CabecalhoAfd cabecalho, IReadOnlyCollection<LinhaMarcacaoAfd> marcacoes)
    {
        ArgumentNullException.ThrowIfNull(cabecalho);
        ArgumentNullException.ThrowIfNull(marcacoes);

        var ordenadas = marcacoes.OrderBy(m => m.Nsr.Valor).ToList();
        var corpo = new StringBuilder();

        // ----- CABECALHO (tipo 1) -----
        // Layout conceitual: [tipo][nsr=000000000][cnpj][dataInicio][dataFim][dataGeracao][horaGeracao][razao]
        // // TODO(validar-oficial): ordem/larguras/zona do cabecalho conforme Anexo do AFD.
        corpo.Append(CampoPosicional.Numero((int)TipoRegistroAfd.Cabecalho, LarguraTipo));
        corpo.Append(CampoPosicional.Numero(0, LarguraNsr)); // cabecalho sem NSR proprio
        corpo.Append(CampoPosicional.Numero(SoDigitos(cabecalho.Cnpj.Digitos), LarguraCnpj));
        corpo.Append(CampoPosicional.Data(cabecalho.InicioPeriodo));
        corpo.Append(CampoPosicional.Data(cabecalho.FimPeriodo));
        corpo.Append(CampoPosicional.Data(DateOnly.FromDateTime(cabecalho.DataHoraGeracao.LocalDateTime.Date)));
        corpo.Append(CampoPosicional.Hora(cabecalho.DataHoraGeracao));
        corpo.Append(CampoPosicional.Texto(cabecalho.RazaoSocial, LarguraRazao));
        corpo.Append(CampoPosicional.FimDeLinha);

        // ----- MARCACOES (tipo 3 = REP-C/REP-A; tipo 7 = REP-P) -----
        // O leiaute 671 distingue a marcacao de REP-C/A (tipo 3) da marcacao de REP-P (tipo 7); o tipo
        // e derivado da origem. // TODO(validar-oficial): posicoes/larguras integrais (CPF 035-046, 12).
        foreach (var m in ordenadas)
        {
            corpo.Append(CampoPosicional.Numero((int)TipoRegistroMarcacao(m.Origem), LarguraTipo));
            corpo.Append(m.Nsr.ParaPosicional(LarguraNsr));
            corpo.Append(CampoPosicional.Data(DateOnly.FromDateTime(m.DataHora.LocalDateTime.Date)));
            corpo.Append(CampoPosicional.Hora(m.DataHora));
            corpo.Append(CampoPosicional.Numero(SoDigitos(m.Cpf.Digitos), LarguraCpf));
            corpo.Append(CampoPosicional.Numero((int)m.Origem, LarguraTipo));
            corpo.Append(CampoPosicional.FimDeLinha);
        }

        // ----- TRAILER (tipo 9) -----
        // Totaliza a quantidade de marcacoes e carrega o CRC-16 do corpo (integridade do AFD).
        // // TODO(validar-oficial): conteudo e abrangencia do CRC (linha-a-linha vs arquivo) na 671.
        var bytesCorpo = CampoPosicional.CodificacaoArquivo.GetBytes(corpo.ToString());
        var crc = Crc16Ccitt.CalcularHex(bytesCorpo);

        corpo.Append(CampoPosicional.Numero((int)TipoRegistroAfd.Trailer, LarguraTipo));
        corpo.Append(CampoPosicional.Numero(ordenadas.Count, LarguraContador));
        corpo.Append(CampoPosicional.Texto(crc, 4));
        corpo.Append(CampoPosicional.FimDeLinha);

        return CampoPosicional.CodificacaoArquivo.GetBytes(corpo.ToString());
    }

    private static long SoDigitos(string valor)
        => long.Parse(new string(valor.Where(char.IsDigit).ToArray()), CultureInfo.InvariantCulture);
}
