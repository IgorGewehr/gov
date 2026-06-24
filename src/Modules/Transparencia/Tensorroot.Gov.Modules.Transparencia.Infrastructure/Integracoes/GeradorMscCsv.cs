using System.Globalization;
using System.IO.Compression;
using System.Text;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;

/// <summary>
/// Gerador da MSC (Matriz de Saldos Contábeis) em CSV adaptado do XBRL-GL, zipado, a partir da
/// <see cref="DeclaracaoFiscal"/> consolidada. Automatizável 100% — NÃO transmite (homologação é ato
/// humano com e-CPF A3 no portal SICONFI).
/// </summary>
/// <remarks>
/// As COLUNAS/atributos EXATOS do CSV/XBRL-GL seguem as Regras Gerais MSC do exercício —
/// <c>// TODO(validar-leiaute-MT-2026)</c>. O cabeçalho abaixo é uma versão mínima (conta;natureza;valor;
/// informação complementar), suficiente para o pipeline e a reconciliação.
/// </remarks>
public sealed class GeradorMscCsv : IGeradorMsc
{
    private static readonly DateTimeOffset DataEntradaFixa = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // TODO(validar-leiaute-MT-2026): cabeçalho/colunas oficiais da MSC (Poder e Órgão, conta corrente etc.).
    private const string CabecalhoCsv = "conta_contabil;natureza_saldo;valor;informacao_complementar";

    /// <inheritdoc />
    public Task<ArtefatoMsc> GerarAsync(DeclaracaoFiscal declaracao, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(declaracao);

        var csv = MontarCsv(declaracao);
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
        return Task.FromResult(new ArtefatoMsc(nomeZip, memoria.ToArray()));
    }

    private static string MontarCsv(DeclaracaoFiscal declaracao)
    {
        var construtor = new StringBuilder();
        construtor.Append(CabecalhoCsv).Append("\r\n");

        foreach (var linha in declaracao.Matriz.Linhas)
        {
            construtor
                .Append(Escapar(linha.ContaPcasp)).Append(';')
                .Append(linha.NaturezaSaldo).Append(';')
                .Append(linha.Valor.Valor.ToString("0.00", CultureInfo.InvariantCulture)).Append(';')
                .Append(Escapar(linha.InformacaoComplementar ?? string.Empty))
                .Append("\r\n");
        }

        return construtor.ToString();
    }

    // Caracteres que, no INICIO de uma celula, fazem o Excel/LibreOffice/Sheets interpretar
    // o conteudo como FORMULA (OWASP CSV Injection). Conta/informacao complementar podem
    // carregar texto de origem externa → neutralizar antes de escapar.
    private static readonly char[] GatilhosFormulaCsv = ['=', '+', '-', '@', '\t', '\r'];

    private static string Escapar(string valor)
    {
        var conteudo = valor.Length > 0 && Array.IndexOf(GatilhosFormulaCsv, valor[0]) >= 0
            ? "'" + valor
            : valor;

        // RFC 4180 §2.6: campo com delimitador, aspas, LF ou CR isolado deve ser citado.
        return conteudo.Contains(';', StringComparison.Ordinal)
            || conteudo.Contains('"', StringComparison.Ordinal)
            || conteudo.Contains('\n', StringComparison.Ordinal)
            || conteudo.Contains('\r', StringComparison.Ordinal)
                ? "\"" + conteudo.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
                : conteudo;
    }
}
