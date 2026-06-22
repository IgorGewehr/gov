namespace Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

/// <summary>
/// Endereco residencial do paciente (owned type): logradouro, numero, bairro, municipio, UF e CEP.
/// Mapeado como owned type do agregado <see cref="Paciente"/>.
/// </summary>
public readonly record struct Endereco
{
    /// <summary>Quantidade exata de caracteres de uma UF.</summary>
    public const int ComprimentoUf = 2;

    /// <summary>Cria um endereco residencial.</summary>
    /// <param name="logradouro">Logradouro (obrigatorio).</param>
    /// <param name="numero">Numero/complemento.</param>
    /// <param name="bairro">Bairro.</param>
    /// <param name="municipio">Municipio.</param>
    /// <param name="uf">Unidade da federacao (2 caracteres).</param>
    /// <param name="cep">CEP (somente digitos).</param>
    /// <exception cref="ArgumentException">Se o logradouro for vazio ou a UF nao tiver 2 caracteres.</exception>
    public Endereco(string logradouro, string numero, string bairro, string municipio, string uf, string cep)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logradouro);
        ArgumentNullException.ThrowIfNull(numero);
        ArgumentNullException.ThrowIfNull(bairro);
        ArgumentNullException.ThrowIfNull(municipio);
        ArgumentException.ThrowIfNullOrWhiteSpace(uf);
        ArgumentNullException.ThrowIfNull(cep);

        var ufNormalizada = uf.Trim().ToUpperInvariant();
        if (ufNormalizada.Length != ComprimentoUf)
        {
            throw new ArgumentException("UF deve ter 2 caracteres.", nameof(uf));
        }

        Logradouro = logradouro.Trim();
        Numero = numero.Trim();
        Bairro = bairro.Trim();
        Municipio = municipio.Trim();
        Uf = ufNormalizada;
        Cep = cep.Trim();
    }

    /// <summary>Logradouro.</summary>
    public string Logradouro { get; }

    /// <summary>Numero/complemento.</summary>
    public string Numero { get; }

    /// <summary>Bairro.</summary>
    public string Bairro { get; }

    /// <summary>Municipio.</summary>
    public string Municipio { get; }

    /// <summary>Unidade da federacao (2 caracteres).</summary>
    public string Uf { get; }

    /// <summary>CEP (somente digitos).</summary>
    public string Cep { get; }
}
