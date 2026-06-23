namespace Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

/// <summary>
/// Endereco do estabelecimento sujeito a VISA. Objeto de Valor imutavel, mapeado como tipo owned/complex
/// na persistencia. Replicado no Bounded Context (isolamento — CLAUDE.md §2): a VISA tem cadastro proprio,
/// independente do endereco do estabelecimento de saude (CNES).
/// </summary>
/// <param name="Logradouro">Logradouro (rua/avenida + numero).</param>
/// <param name="Bairro">Bairro.</param>
/// <param name="Municipio">Municipio.</param>
/// <param name="Uf">Unidade Federativa (2 letras).</param>
/// <param name="Cep">CEP (somente digitos).</param>
public readonly record struct EnderecoVisa(
    string Logradouro,
    string Bairro,
    string Municipio,
    string Uf,
    string Cep)
{
    /// <summary>Comprimento exato da UF.</summary>
    public const int ComprimentoUf = 2;

    /// <summary>Endereco vazio (placeholder de materializacao/EF).</summary>
    public static EnderecoVisa Vazio => new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

    /// <summary>Cria um endereco validando os campos minimos.</summary>
    /// <param name="logradouro">Logradouro.</param>
    /// <param name="bairro">Bairro.</param>
    /// <param name="municipio">Municipio.</param>
    /// <param name="uf">UF (2 letras).</param>
    /// <param name="cep">CEP.</param>
    /// <returns>Endereco valido.</returns>
    /// <exception cref="ArgumentException">Se logradouro/municipio forem vazios ou a UF nao tiver 2 letras.</exception>
    public static EnderecoVisa Criar(string logradouro, string bairro, string municipio, string uf, string cep)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logradouro);
        ArgumentException.ThrowIfNullOrWhiteSpace(municipio);
        ArgumentException.ThrowIfNullOrWhiteSpace(uf);
        if (uf.Trim().Length != ComprimentoUf)
        {
            throw new ArgumentException("UF deve ter 2 caracteres.", nameof(uf));
        }

        return new EnderecoVisa(
            logradouro.Trim(),
            (bairro ?? string.Empty).Trim(),
            municipio.Trim(),
            uf.Trim().ToUpperInvariant(),
            new string([.. (cep ?? string.Empty).Where(char.IsAsciiDigit)]));
    }
}
