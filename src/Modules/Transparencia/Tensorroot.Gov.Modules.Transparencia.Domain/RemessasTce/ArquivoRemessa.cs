using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

/// <summary>Identificador forte de um <see cref="ArquivoRemessa"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ArquivoRemessaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ArquivoRemessaId"/>.</returns>
    public static ArquivoRemessaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Arquivo componente do pacote de remessa, conforme o leiaute (entidade-filha). Carrega o
/// conteúdo (ou referência a blob), o <see cref="HashIntegridade"/> próprio e os
/// <see cref="RegistroLeiaute"/> estruturados.
/// </summary>
public sealed class ArquivoRemessa : Entity<ArquivoRemessaId>
{
    private readonly List<RegistroLeiaute> _registros = [];

    private ArquivoRemessa()
    {
    }

    private ArquivoRemessa(ArquivoRemessaId id, string nomeArquivo, ReadOnlyMemory<byte> conteudo, HashIntegridade hash)
        : base(id)
    {
        NomeArquivo = nomeArquivo;
        Conteudo = conteudo;
        Hash = hash;
    }

    /// <summary>Nome do arquivo componente (não vazio).</summary>
    public string NomeArquivo { get; private set; } = default!;

    /// <summary>Conteúdo do arquivo (ou referência a blob).</summary>
    public ReadOnlyMemory<byte> Conteudo { get; private set; }

    /// <summary>Hash de integridade do arquivo.</summary>
    public HashIntegridade Hash { get; private set; } = default!;

    /// <summary>Linhas estruturadas do arquivo conforme o leiaute.</summary>
    public IReadOnlyCollection<RegistroLeiaute> Registros => _registros;

    /// <summary>Cria um arquivo de remessa e calcula seu hash de integridade sobre o conteúdo.</summary>
    /// <param name="nomeArquivo">Nome do arquivo (não vazio).</param>
    /// <param name="conteudo">Conteúdo do arquivo.</param>
    /// <param name="registros">Registros estruturados do arquivo.</param>
    /// <returns>Novo <see cref="ArquivoRemessa"/>.</returns>
    /// <exception cref="ArgumentException">Se o nome for vazio.</exception>
    public static ArquivoRemessa Criar(
        string nomeArquivo,
        ReadOnlyMemory<byte> conteudo,
        IEnumerable<RegistroLeiaute> registros)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeArquivo);
        ArgumentNullException.ThrowIfNull(registros);

        var hash = HashIntegridade.Calcular(conteudo.Span);
        var arquivo = new ArquivoRemessa(ArquivoRemessaId.New(), nomeArquivo, conteudo, hash);
        arquivo._registros.AddRange(registros);
        return arquivo;
    }
}
