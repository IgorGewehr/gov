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
    private const int LarguraTipo = 1;
    private const int LarguraNsr = Nsr.LarguraPadrao;
    private const int LarguraCnpj = 14;
    private const int LarguraCpf = 11;
    private const int LarguraRazao = 150;
    private const int LarguraContador = 9;

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

        // ----- MARCACOES (tipo 3) -----
        foreach (var m in ordenadas)
        {
            corpo.Append(CampoPosicional.Numero((int)TipoRegistroAfd.Marcacao, LarguraTipo));
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
