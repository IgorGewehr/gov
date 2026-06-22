using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor que representa um endereco vinculado a um territorio (area de cobertura de
/// um CRAS). O territorio e a chave de pertencimento da familia a uma <c>UnidadeAtendimento</c>.
/// </summary>
public sealed class EnderecoTerritorializado : ValueObject
{
    private EnderecoTerritorializado(string logradouro, string municipio, string cep, string territorio)
    {
        Logradouro = logradouro;
        Municipio = municipio;
        Cep = cep;
        Territorio = territorio;
    }

    /// <summary>Logradouro (rua, numero, complemento).</summary>
    public string Logradouro { get; }

    /// <summary>Municipio do endereco.</summary>
    public string Municipio { get; }

    /// <summary>CEP (somente digitos).</summary>
    public string Cep { get; }

    /// <summary>Territorio (area de abrangencia do CRAS) ao qual o endereco pertence.</summary>
    public string Territorio { get; }

    /// <summary>Cria um endereco territorializado.</summary>
    /// <param name="logradouro">Logradouro.</param>
    /// <param name="municipio">Municipio.</param>
    /// <param name="cep">CEP (com ou sem mascara).</param>
    /// <param name="territorio">Territorio de cobertura do CRAS.</param>
    /// <returns>Instancia de <see cref="EnderecoTerritorializado"/>.</returns>
    /// <exception cref="ArgumentException">Quando algum campo obrigatorio e vazio.</exception>
    public static EnderecoTerritorializado Create(string logradouro, string municipio, string cep, string territorio)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logradouro);
        ArgumentException.ThrowIfNullOrWhiteSpace(municipio);
        ArgumentException.ThrowIfNullOrWhiteSpace(cep);
        ArgumentException.ThrowIfNullOrWhiteSpace(territorio);
        return new EnderecoTerritorializado(logradouro, municipio, ExtrairDigitos(cep), territorio);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Logradouro}, {Municipio} ({Territorio})";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Logradouro;
        yield return Municipio;
        yield return Cep;
        yield return Territorio;
    }

    private static string ExtrairDigitos(string value)
    {
        Span<char> buffer = value.Length <= 16 ? stackalloc char[value.Length] : new char[value.Length];
        var count = 0;
        foreach (var c in value)
        {
            if (char.IsAsciiDigit(c))
            {
                buffer[count++] = c;
            }
        }

        return new string(buffer[..count]);
    }
}
