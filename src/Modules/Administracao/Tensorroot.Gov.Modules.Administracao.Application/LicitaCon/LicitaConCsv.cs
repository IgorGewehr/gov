using System.Globalization;
using System.Text;

namespace Tensorroot.Gov.Modules.Administracao.Application.LicitaCon;

/// <summary>
/// Escritor de uma tabela CSV no formato do leiaute LicitaCon 1.4 (e-Validador TCE-RS): separador VIRGULA,
/// codificacao UTF-8 com BOM, terminador de linha CRLF, primeira linha = cabecalho com os nomes oficiais
/// das colunas (prefixos CD_/NR_/TP_/DT_/BL_/VL_/PC_/SG_/NM_/DS_). Campos com virgula, aspas ou quebra de
/// linha sao envolvidos em aspas duplas (RFC 4180, "" para escapar aspas). Datas em <c>yyyy-MM-dd</c>;
/// decimais com PONTO e 2 casas; nulos como vazio — exatamente como nas remessas reais publicadas pelo
/// TCE-RS no dados.tce.rs.gov.br. Estrutura de coluna confirmada ao vivo contra arquivo real do orgao;
/// // TODO(M10-validate): conferir o conjunto exato de colunas/dominios de cada um dos 14 arquivos contra
/// o PDF oficial do leiaute 1.4 no credenciamento do e-Validador.
/// </summary>
public sealed class LicitaConCsvEscritor
{
    private readonly string[] _colunas;
    private readonly List<string?[]> _linhas = [];

    /// <summary>Cria o escritor de uma tabela com as colunas (cabecalho oficial) informadas.</summary>
    /// <param name="colunas">Nomes oficiais das colunas (na ordem do leiaute).</param>
    /// <exception cref="ArgumentException">Se a lista de colunas for vazia.</exception>
    public LicitaConCsvEscritor(params string[] colunas)
    {
        ArgumentNullException.ThrowIfNull(colunas);
        if (colunas.Length == 0)
        {
            throw new ArgumentException("Ao menos uma coluna e obrigatoria.", nameof(colunas));
        }

        _colunas = colunas;
    }

    /// <summary>Quantidade de colunas (largura do cabecalho).</summary>
    public int QuantidadeColunas => _colunas.Length;

    /// <summary>Quantidade de linhas de dados adicionadas (sem o cabecalho).</summary>
    public int QuantidadeLinhas => _linhas.Count;

    /// <summary>
    /// Adiciona uma linha de dados. A quantidade de valores deve casar com a quantidade de colunas
    /// (cada celula ja formatada como string oficial ou <c>null</c> para vazio — use os helpers
    /// <see cref="Data"/>, <see cref="Valor"/>, <see cref="Booleano"/>, <see cref="Inteiro"/>).
    /// </summary>
    /// <param name="valores">Celulas da linha, na ordem das colunas.</param>
    /// <exception cref="ArgumentException">Se a aridade nao casar com as colunas.</exception>
    public void AdicionarLinha(params string?[] valores)
    {
        ArgumentNullException.ThrowIfNull(valores);
        if (valores.Length != _colunas.Length)
        {
            throw new ArgumentException(
                $"A linha tem {valores.Length} celulas, mas o cabecalho tem {_colunas.Length} colunas.",
                nameof(valores));
        }

        _linhas.Add(valores);
    }

    /// <summary>Serializa a tabela completa (cabecalho + linhas) como texto CSV (sem BOM — o BOM e do arquivo).</summary>
    /// <returns>Conteudo CSV com terminador CRLF.</returns>
    public string Serializar()
    {
        var sb = new StringBuilder();
        sb.Append(string.Join(',', _colunas.Select(Escapar))).Append("\r\n");
        foreach (var linha in _linhas)
        {
            sb.Append(string.Join(',', linha.Select(Escapar))).Append("\r\n");
        }

        return sb.ToString();
    }

    /// <summary>Codifica a tabela em bytes UTF-8 COM BOM (como exige o e-Validador para arquivos com acento).</summary>
    /// <returns>Bytes do arquivo CSV (BOM + conteudo).</returns>
    public byte[] SerializarBytes()
    {
        var conteudo = Serializar();
        var bom = Encoding.UTF8.GetPreamble();
        var corpo = Encoding.UTF8.GetBytes(conteudo);
        var buffer = new byte[bom.Length + corpo.Length];
        Buffer.BlockCopy(bom, 0, buffer, 0, bom.Length);
        Buffer.BlockCopy(corpo, 0, buffer, bom.Length, corpo.Length);
        return buffer;
    }

    /// <summary>Formata uma data no padrao do leiaute (<c>yyyy-MM-dd</c>); nulo => vazio.</summary>
    /// <param name="data">Data a formatar.</param>
    /// <returns>Data formatada ou <c>null</c>.</returns>
    public static string? Data(DateOnly? data)
        => data?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Formata um valor monetario/percentual com PONTO decimal e 2 casas; nulo => vazio.</summary>
    /// <param name="valor">Valor a formatar.</param>
    /// <returns>Valor formatado ou <c>null</c>.</returns>
    public static string? Valor(decimal? valor)
        => valor?.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>Formata um inteiro; nulo => vazio.</summary>
    /// <param name="numero">Inteiro a formatar.</param>
    /// <returns>Inteiro formatado ou <c>null</c>.</returns>
    public static string? Inteiro(int? numero)
        => numero?.ToString(CultureInfo.InvariantCulture);

    /// <summary>Formata um booleano no padrao do leiaute (S/N); nulo => vazio.</summary>
    /// <param name="valor">Booleano a formatar.</param>
    /// <returns>"S"/"N" ou <c>null</c>.</returns>
    public static string? Booleano(bool? valor)
        => valor is null ? null : valor.Value ? "S" : "N";

    private static string Escapar(string? celula)
    {
        if (string.IsNullOrEmpty(celula))
        {
            return string.Empty;
        }

        var precisaAspas = celula.Contains(',', StringComparison.Ordinal)
            || celula.Contains('"', StringComparison.Ordinal)
            || celula.Contains('\n', StringComparison.Ordinal)
            || celula.Contains('\r', StringComparison.Ordinal);

        if (!precisaAspas)
        {
            return celula;
        }

        return string.Concat("\"", celula.Replace("\"", "\"\"", StringComparison.Ordinal), "\"");
    }
}
