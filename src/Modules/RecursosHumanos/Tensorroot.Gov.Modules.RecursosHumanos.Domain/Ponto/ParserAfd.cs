using System.Globalization;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

/// <summary>Uma marcacao lida (parseada) de um AFD de equipamento, ainda nao vinculada a um servidor.</summary>
/// <param name="NsrEquipamento">NSR original gravado no equipamento (chave natural de idempotencia).</param>
/// <param name="Cpf">CPF do trabalhador lido do registro.</param>
/// <param name="DataHora">Data/hora da batida (montada de data + hora do registro).</param>
/// <param name="Origem">Origem derivada do tipo de registro (3=REP-C/A; 7=REP-P).</param>
public sealed record RegistroAfdParseado(long NsrEquipamento, Cpf Cpf, DateTimeOffset DataHora, TipoRep Origem);

/// <summary>Cabecalho lido do AFD (identificacao do empregador e periodo).</summary>
/// <param name="CnpjDigitos">CNPJ do empregador (so digitos, como lido).</param>
/// <param name="RazaoSocial">Razao social/nome do ente.</param>
/// <param name="InicioPeriodo">Primeira data do periodo.</param>
/// <param name="FimPeriodo">Ultima data do periodo.</param>
public sealed record CabecalhoAfdParseado(string CnpjDigitos, string RazaoSocial, DateOnly InicioPeriodo, DateOnly FimPeriodo);

/// <summary>
/// Resultado do parse de um AFD: cabecalho, marcacoes lidas e os veredictos de INTEGRIDADE
/// (CRC-16 do arquivo, continuidade de NSR, contador do trailer). O handler de ingestao decide
/// FAIL-CLOSED com base nestes campos (CLAUDE.md §16).
/// </summary>
/// <param name="Cabecalho">Cabecalho lido.</param>
/// <param name="Marcacoes">Marcacoes lidas (na ordem do arquivo).</param>
/// <param name="CrcOk">Verdadeiro se o CRC-16 recalculado bate com o do trailer.</param>
/// <param name="NsrContinuo">Verdadeiro se os NSR de equipamento sao crescentes e sem lacunas.</param>
/// <param name="ContadorTrailerOk">Verdadeiro se o contador do trailer = quantidade de marcacoes.</param>
/// <param name="PrimeiroNsr">Menor NSR de equipamento lido (<c>null</c> se vazio).</param>
/// <param name="UltimoNsr">Maior NSR de equipamento lido (<c>null</c> se vazio).</param>
public sealed record ResultadoParseAfd(
    CabecalhoAfdParseado Cabecalho,
    IReadOnlyList<RegistroAfdParseado> Marcacoes,
    bool CrcOk,
    bool NsrContinuo,
    bool ContadorTrailerOk,
    long? PrimeiroNsr,
    long? UltimoNsr)
{
    /// <summary>Integridade COMPLETA do arquivo (CRC + NSR contiguo + contador do trailer).</summary>
    public bool IntegridadeOk => CrcOk && NsrContinuo && ContadorTrailerOk;
}

/// <summary>Servico de parse do AFD (puro, sem I/O) — inverso do <see cref="GeradorAfd"/>.</summary>
public interface IParserAfd
{
    /// <summary>
    /// Le o AFD posicional (ISO-8859-1) e valida integridade (CRC/NSR/contador). // TODO(validar-oficial):
    /// posicoes/larguras integrais do Anexo da Portaria 671 (CPF 035-046, tipos 3/7, abrangencia do CRC).
    /// </summary>
    /// <param name="afd">Bytes do AFD (ISO-8859-1).</param>
    /// <returns>Resultado com marcacoes lidas e veredictos de integridade.</returns>
    /// <exception cref="FormatException">Se a estrutura/larguras nao corresponderem ao leiaute esperado.</exception>
    ResultadoParseAfd Parse(ReadOnlySpan<byte> afd);
}

/// <summary>
/// Parser do AFD (Arquivo Fonte de Dados — Portaria MTP 671/2021): le o arquivo posicional ASCII
/// (ISO-8859-1) produzido por qualquer REP-C/A/P, FIEL ao mesmo leiaute do <see cref="GeradorAfd"/>
/// (mesmas larguras/CRC — fonte unica de verdade). Servico de dominio PURO (sem I/O). Valida o CRC-16
/// do trailer, a continuidade de NSR (lacuna = sinal de supressao, marca o lote como suspeito) e o
/// contador do trailer. // TODO(validar-oficial): posicoes/larguras integrais do Anexo oficial (DOU).
/// </summary>
public sealed class ParserAfd : IParserAfd
{
    /// <inheritdoc />
    public ResultadoParseAfd Parse(ReadOnlySpan<byte> afd)
    {
        // Decodifica em ISO-8859-1 (Latin1) — mesma codificacao do gerador.
        var texto = CampoPosicional.CodificacaoArquivo.GetString(afd);
        var linhas = texto.Split(CampoPosicional.FimDeLinha, StringSplitOptions.RemoveEmptyEntries);
        if (linhas.Length < 2)
        {
            throw new FormatException("AFD invalido: esperados ao menos cabecalho e trailer.");
        }

        CabecalhoAfdParseado? cabecalho = null;
        var marcacoes = new List<RegistroAfdParseado>();
        string? crcTrailer = null;
        long contadorTrailer = -1;
        var nsrContinuo = true;
        long? primeiroNsr = null;
        long? ultimoNsr = null;
        long? nsrAnterior = null;

        // O CRC do gerador cobre o CORPO (cabecalho + marcacoes), ANTES do trailer; recalculamos sobre
        // exatamente os mesmos bytes. // TODO(validar-oficial): abrangencia do CRC na 671 (corpo vs arquivo).
        var corpoParaCrc = new System.Text.StringBuilder();

        foreach (var linha in linhas)
        {
            if (linha.Length < GeradorAfd.LarguraTipo)
            {
                throw new FormatException("AFD invalido: linha sem tipo de registro.");
            }

            var tipo = (int)CampoPosicional.LerNumero(linha.AsSpan(0, GeradorAfd.LarguraTipo));

            if (tipo == (int)TipoRegistroAfd.Cabecalho)
            {
                cabecalho = LerCabecalho(linha);
                corpoParaCrc.Append(linha).Append(CampoPosicional.FimDeLinha);
            }
            else if (GeradorAfd.EhMarcacao(tipo))
            {
                var registro = LerMarcacao(linha, tipo);
                marcacoes.Add(registro);

                primeiroNsr ??= registro.NsrEquipamento;
                ultimoNsr = registro.NsrEquipamento;
                if (nsrAnterior is { } anterior && registro.NsrEquipamento != anterior + 1)
                {
                    nsrContinuo = false; // lacuna ou nao-monotonicidade: lote suspeito (auditavel).
                }

                nsrAnterior = registro.NsrEquipamento;
                corpoParaCrc.Append(linha).Append(CampoPosicional.FimDeLinha);
            }
            else if (tipo == (int)TipoRegistroAfd.Trailer)
            {
                (contadorTrailer, crcTrailer) = LerTrailer(linha);

                // O trailer NAO entra no calculo do CRC (igual ao gerador).
            }
            else
            {
                // Tipos nao tratados (ajuste de relogio, empregado, etc.) entram no corpo do CRC para
                // manter fidelidade ao arquivo, mas nao geram marcacao. // TODO(validar-oficial).
                corpoParaCrc.Append(linha).Append(CampoPosicional.FimDeLinha);
            }
        }

        if (cabecalho is null)
        {
            throw new FormatException("AFD invalido: cabecalho (tipo 1) ausente.");
        }

        if (crcTrailer is null)
        {
            throw new FormatException("AFD invalido: trailer (tipo 9) ausente.");
        }

        var bytesCorpo = CampoPosicional.CodificacaoArquivo.GetBytes(corpoParaCrc.ToString());
        var crcCalculado = Crc16Ccitt.CalcularHex(bytesCorpo);
        var crcOk = string.Equals(crcCalculado, crcTrailer, StringComparison.OrdinalIgnoreCase);
        var contadorOk = contadorTrailer == marcacoes.Count;

        return new ResultadoParseAfd(
            cabecalho,
            marcacoes,
            crcOk,
            nsrContinuo,
            contadorOk,
            primeiroNsr,
            ultimoNsr);
    }

    private static CabecalhoAfdParseado LerCabecalho(string linha)
    {
        // Layout (inverso do gerador): [tipo:1][nsr:9][cnpj:14][dataIni:8][dataFim:8][dataGer:8][hora:4][razao:150]
        var pos = GeradorAfd.LarguraTipo + GeradorAfd.LarguraNsr;
        var minimo = pos + GeradorAfd.LarguraCnpj + (GeradorAfd.LarguraData * 3) + GeradorAfd.LarguraHora;
        if (linha.Length < minimo)
        {
            throw new FormatException("AFD invalido: cabecalho com largura insuficiente.");
        }

        var cnpj = linha.AsSpan(pos, GeradorAfd.LarguraCnpj);
        pos += GeradorAfd.LarguraCnpj;
        var inicio = CampoPosicional.LerData(linha.AsSpan(pos, GeradorAfd.LarguraData));
        pos += GeradorAfd.LarguraData;
        var fim = CampoPosicional.LerData(linha.AsSpan(pos, GeradorAfd.LarguraData));
        pos += GeradorAfd.LarguraData;
        pos += GeradorAfd.LarguraData; // dataGeracao (nao usada na ingestao)
        pos += GeradorAfd.LarguraHora; // horaGeracao (nao usada na ingestao)

        var razao = pos < linha.Length ? CampoPosicional.LerTexto(linha.AsSpan(pos)) : string.Empty;
        return new CabecalhoAfdParseado(cnpj.ToString(), razao, inicio, fim);
    }

    private static RegistroAfdParseado LerMarcacao(string linha, int tipo)
    {
        // Layout (inverso do gerador): [tipo:1][nsr:9][data:8][hora:4][cpf:11][origem:1]
        var minimo = GeradorAfd.LarguraTipo + GeradorAfd.LarguraNsr + GeradorAfd.LarguraData
            + GeradorAfd.LarguraHora + GeradorAfd.LarguraCpf + GeradorAfd.LarguraTipo;
        if (linha.Length < minimo)
        {
            throw new FormatException("AFD invalido: registro de marcacao com largura insuficiente.");
        }

        var pos = GeradorAfd.LarguraTipo;
        var nsr = CampoPosicional.LerNumero(linha.AsSpan(pos, GeradorAfd.LarguraNsr));
        pos += GeradorAfd.LarguraNsr;
        var data = CampoPosicional.LerData(linha.AsSpan(pos, GeradorAfd.LarguraData));
        pos += GeradorAfd.LarguraData;
        var hora = CampoPosicional.LerHora(linha.AsSpan(pos, GeradorAfd.LarguraHora));
        pos += GeradorAfd.LarguraHora;
        var cpfDigitos = linha.AsSpan(pos, GeradorAfd.LarguraCpf).ToString();
        pos += GeradorAfd.LarguraCpf;
        var origem = (TipoRep)(int)CampoPosicional.LerNumero(linha.AsSpan(pos, GeradorAfd.LarguraTipo));

        if (!Enum.IsDefined(origem))
        {
            // Falha-fechada: origem fora do dominio do AFD (CLAUDE.md §16). Cai antes de tocar o banco.
            throw new FormatException(
                string.Format(CultureInfo.InvariantCulture, "AFD invalido: origem '{0}' fora do dominio.", (int)origem));
        }

        var dataHora = new DateTimeOffset(data.ToDateTime(hora), TimeSpan.Zero);
        var cpf = Cpf.Create(cpfDigitos);

        // Coerencia tipo x origem (3=REP-C/A; 7=REP-P) — apenas auditavel; nao bloqueia o parse aqui.
        _ = tipo;
        return new RegistroAfdParseado(nsr, cpf, dataHora, origem);
    }

    private static (long Contador, string Crc) LerTrailer(string linha)
    {
        // Layout (inverso do gerador): [tipo:1][contador:9][crc:4]
        var minimo = GeradorAfd.LarguraTipo + GeradorAfd.LarguraContador + GeradorAfd.LarguraCrc;
        if (linha.Length < minimo)
        {
            throw new FormatException("AFD invalido: trailer com largura insuficiente.");
        }

        var pos = GeradorAfd.LarguraTipo;
        var contador = CampoPosicional.LerNumero(linha.AsSpan(pos, GeradorAfd.LarguraContador));
        pos += GeradorAfd.LarguraContador;
        var crc = linha.AsSpan(pos, GeradorAfd.LarguraCrc).ToString();
        return (contador, crc);
    }
}
