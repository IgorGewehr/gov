using System.Globalization;
using System.IO.Compression;
using System.Text;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;

/// <summary>
/// Gerador da MSC (Matriz de Saldos Contábeis) no leiaute CSV oficial do SICONFI (Regras Gerais MSC 2026 —
/// Anexo I da Portaria STN 642/2019, §"Arquivo CSV"), zipado, a partir da <see cref="DeclaracaoFiscal"/>
/// consolidada. Automatizável 100% — NÃO transmite (a homologação é ato humano com e-CPF A3 no portal
/// SICONFI).
/// </summary>
/// <remarks>
/// Leiaute oficial (18 colunas, separador <c>|</c>): Periodo, Cod.Siconfi, NaturezaInformacao, Conta,
/// Tipo_Valor, Valor, e SEIS pares de informação complementar (TIPO1/IC1 .. TIPO6/IC6). O
/// <c>Tipo_Valor</c> usa os literais da taxonomia XBRL GL (<c>saldo_inicial</c>/<c>movimento</c>/
/// <c>saldo_final</c>); o <c>Cod.Siconfi</c> é o IBGE+<c>"EX"</c> do ente; o <c>Periodo</c> é
/// <c>YYYY-MM</c> (mensal) ou <c>YYYY-13</c> (encerramento/anual). A declaração consolidada na
/// Transparência guarda apenas as linhas de SALDO FINAL (a ACL filtra TipoValor=ending_balance), logo todas
/// as linhas saem como <c>saldo_final</c>; saldo inicial/movimento por conta são detalhados no M4.
/// // TODO(M10-validate): confirmar separador/ordem/codigos das tabelas TIPO contra o e-Validador SICONFI.
/// </remarks>
public sealed class GeradorMscCsv(IIdentificacaoEnteSiconfi identificacaoEnte) : IGeradorMsc
{
    private static readonly DateTimeOffset DataEntradaFixa = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // Separador oficial do CSV da MSC (pipe). Os valores nao contem pipe (conta/valor/atributos numericos).
    private const char Separador = '|';

    // Numero fixo de pares de informacao complementar (TIPOx/ICx) do leiaute oficial.
    private const int QuantidadeInformacoesComplementares = 6;

    // Mes 13 = MSC de Encerramento/anual (Periodo YYYY-13). DeclaracaoFiscal sem Competencia => exercicio.
    private const int MesEncerramento = 13;

    // Sufixo do Poder Executivo no Cod.Siconfi (a MSC e enviada SO pelo Executivo).
    private const string SufixoExecutivo = "EX";

    // Cabecalho oficial: 6 colunas fixas + 6 pares TIPOx/ICx.
    private static readonly string CabecalhoCsv = MontarCabecalho();

    /// <inheritdoc />
    public async Task<ArtefatoMsc> GerarAsync(DeclaracaoFiscal declaracao, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(declaracao);

        // FAIL-CLOSED (P0-1): aplica as validacoes DURAS do SICONFI ANTES de emitir. A ponte de integracao
        // Financas->Transparencia descarta as linhas de saldo inicial/movimento e a estrutura de PO; logo o
        // CSV NAO pode herdar as validacoes do agregado de Financas. Re-impomos aqui (PO obrigatorio por
        // linha, balanco D=C por classe e global) sobre os dados que o CSV possui. Lanca se inconsistente —
        // jamais gerar um artefato que o e-Validador SICONFI rejeitaria.
        ValidadorMscCsv.GarantirValida(declaracao.Matriz, ExtrairPoderOrgao);

        var codigoSiconfi = await identificacaoEnte
            .ObterCodigoSiconfiAsync(cancellationToken)
            .ConfigureAwait(false);

        var periodo = MontarPeriodo(declaracao);
        var csv = MontarCsv(declaracao, periodo, codigoSiconfi);
        var bytesCsv = Encoding.UTF8.GetBytes(csv);
        var nomeBaseCsv = string.Create(
            CultureInfo.InvariantCulture,
            $"MSC_{declaracao.TipoDeclaracao}_{declaracao.Exercicio}.csv");

        using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entrada = zip.CreateEntry(nomeBaseCsv, CompressionLevel.Optimal);
            entrada.LastWriteTime = DataEntradaFixa;
            using var fluxo = entrada.Open();
            fluxo.Write(bytesCsv);
        }

        var nomeZip = string.Create(
            CultureInfo.InvariantCulture,
            $"MSC_{declaracao.TipoDeclaracao}_{declaracao.Exercicio}.zip");
        return new ArtefatoMsc(nomeZip, memoria.ToArray());
    }

    // Periodo YYYY-MM (mensal) ou YYYY-13 (encerramento/anual: declaracao sem Competencia).
    private static string MontarPeriodo(DeclaracaoFiscal declaracao)
    {
        var mes = declaracao.Competencia?.Mes ?? MesEncerramento;
        return string.Create(CultureInfo.InvariantCulture, $"{declaracao.Exercicio:0000}-{mes:00}");
    }

    private static string MontarCsv(DeclaracaoFiscal declaracao, string periodo, string codigoSiconfi)
    {
        var construtor = new StringBuilder();
        construtor.Append(CabecalhoCsv).Append("\r\n");

        var codSiconfi = NormalizarCodigoSiconfi(codigoSiconfi);

        foreach (var linha in declaracao.Matriz.Linhas)
        {
            var naturezaInformacao = NaturezaInformacaoDe(linha.ContaPcasp);
            var slots = MontarSlotsInformacoesComplementares(linha.InformacaoComplementar);

            construtor
                .Append(periodo).Append(Separador)
                .Append(codSiconfi).Append(Separador)
                .Append(naturezaInformacao).Append(Separador)
                .Append(Escapar(linha.ContaPcasp)).Append(Separador)
                .Append("saldo_final").Append(Separador) // unica especie consolidada nesta camada.
                .Append(linha.Valor.Valor.ToString("0.00", CultureInfo.InvariantCulture));

            // Emite SEMPRE os 6 pares (preenchidos ou vazios) em SLOT FIXO por codigo — o PO sai SEMPRE em
            // TIPO1/IC1 (P0-2); o numero/ordem de colunas e FIXO no leiaute oficial e NAO segue a ordem do
            // texto livre das informacoes complementares.
            for (var i = 0; i < QuantidadeInformacoesComplementares; i++)
            {
                var (tipo, valor) = slots[i];
                construtor
                    .Append(Separador).Append(Escapar(tipo))
                    .Append(Separador).Append(Escapar(valor));
            }

            construtor.Append("\r\n");
        }

        return construtor.ToString();
    }

    // Cabecalho: Periodo|Cod.Siconfi|NaturezaInformacao|Conta|Tipo_Valor|Valor|TIPO1|IC1|...|TIPO6|IC6.
    private static string MontarCabecalho()
    {
        var construtor = new StringBuilder("Periodo|Cod.Siconfi|NaturezaInformacao|Conta|Tipo_Valor|Valor");
        for (var i = 1; i <= QuantidadeInformacoesComplementares; i++)
        {
            construtor.Append(Separador).Append("TIPO").Append(i.ToString(CultureInfo.InvariantCulture));
            construtor.Append(Separador).Append("IC").Append(i.ToString(CultureInfo.InvariantCulture));
        }

        return construtor.ToString();
    }

    // NaturezaInformacao (classe contabil) a partir do 1o digito da conta PCASP (PCASP §3):
    // 1-4 Patrimonial, 5-6 Orcamentaria, 7-8 Controle.
    private static string NaturezaInformacaoDe(string contaPcasp)
        => PrimeiroDigito(contaPcasp) switch
        {
            >= 1 and <= 4 => "Patrimonial",
            5 or 6 => "Orcamentaria",
            _ => "Controle",
        };

    private static int PrimeiroDigito(string contaPcasp)
    {
        foreach (var c in contaPcasp)
        {
            if (char.IsAsciiDigit(c))
            {
                return c - '0';
            }
        }

        return 7;
    }

    // Garante o sufixo "EX" (Executivo) no Cod.Siconfi se o provedor devolver so o IBGE.
    private static string NormalizarCodigoSiconfi(string codigoSiconfi)
    {
        var bruto = (codigoSiconfi ?? string.Empty).Trim();
        return bruto.EndsWith(SufixoExecutivo, StringComparison.OrdinalIgnoreCase)
            ? bruto
            : bruto + SufixoExecutivo;
    }

    // Ordem FIXA dos codigos de informacao complementar (IC) nos slots do leiaute oficial: o Poder/Orgao
    // (PO) e SEMPRE o primeiro (TIPO1/IC1), seguido dos demais atributos do quadro MSC (Anexo II Port. STN
    // 642/2019) em ordem canonica. O slot e por CODIGO, nunca pela ordem do texto livre (P0-2). Sao 9
    // codigos para 6 slots: os 6 primeiros presentes (na ordem canonica, com PO garantido em 1º) sao emitidos.
    private static readonly string[] OrdemSlotsIc = ["PO", "FP", "DC", "FR", "CO", "NR", "ND", "FS", "AI"];

    // Monta os 6 pares (TIPO, IC) do leiaute em SLOT FIXO por codigo: percorre a ordem canonica
    // (PO primeiro) e emite os codigos PRESENTES; os slots restantes saem vazios. Garante PO em TIPO1/IC1.
    private static (string Tipo, string Valor)[] MontarSlotsInformacoesComplementares(string? texto)
    {
        var atributos = ParsearInformacoesComplementares(texto);
        var slots = new (string Tipo, string Valor)[QuantidadeInformacoesComplementares];
        Array.Fill(slots, (string.Empty, string.Empty));

        var slot = 0;
        foreach (var codigo in OrdemSlotsIc)
        {
            if (slot >= QuantidadeInformacoesComplementares)
            {
                break;
            }

            if (atributos.TryGetValue(codigo, out var valor) && !string.IsNullOrWhiteSpace(valor))
            {
                slots[slot] = (codigo, valor);
                slot++;
            }
        }

        return slots;
    }

    // Extrai o valor do Poder/Orgao (PO) do texto canonico — usado pela validacao fail-closed (P0-1).
    private static string? ExtrairPoderOrgao(string? texto)
        => ParsearInformacoesComplementares(texto).GetValueOrDefault(ValidadorMscCsv.CodigoIcPoderOrgao);

    // Converte o texto canonico "CHAVE=valor;CHAVE=valor" (de InformacoesComplementaresMsc.ParaTexto) num
    // mapa codigo->valor (PO/FP/DC/FR/CO/NR/ND/FS/AI). O ultimo valor vence em caso de chave repetida.
    private static Dictionary<string, string> ParsearInformacoesComplementares(string? texto)
    {
        var atributos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(texto))
        {
            return atributos;
        }

        foreach (var parte in texto.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var igual = parte.IndexOf('=', StringComparison.Ordinal);
            if (igual <= 0)
            {
                continue;
            }

            atributos[parte[..igual]] = parte[(igual + 1)..];
        }

        return atributos;
    }

    // Caracteres que, no INICIO de uma celula, fazem o Excel/LibreOffice/Sheets interpretar
    // o conteudo como FORMULA (OWASP CSV Injection). Conta/atributos podem carregar texto de
    // origem externa -> neutralizar antes de escapar.
    private static readonly char[] GatilhosFormulaCsv = ['=', '+', '-', '@', '\t', '\r'];

    private static string Escapar(string valor)
    {
        var conteudo = valor.Length > 0 && Array.IndexOf(GatilhosFormulaCsv, valor[0]) >= 0
            ? "'" + valor
            : valor;

        // Campo com o delimitador, aspas, LF ou CR isolado deve ser citado.
        return conteudo.Contains(Separador, StringComparison.Ordinal)
            || conteudo.Contains('"', StringComparison.Ordinal)
            || conteudo.Contains('\n', StringComparison.Ordinal)
            || conteudo.Contains('\r', StringComparison.Ordinal)
                ? "\"" + conteudo.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
                : conteudo;
    }
}
