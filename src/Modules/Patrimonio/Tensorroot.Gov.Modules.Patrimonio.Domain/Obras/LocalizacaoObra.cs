using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

/// <summary>
/// Localização da obra (objeto de valor): logradouro, município/UF, coordenadas opcionais e
/// código geográfico/SICOE quando aplicável. Imutável; nasce válida (município/UF obrigatórios).
/// </summary>
public sealed class LocalizacaoObra : ValueObject
{
    private LocalizacaoObra(
        string logradouro,
        string municipio,
        string uf,
        decimal? latitude,
        decimal? longitude,
        string? geoCodigo)
    {
        Logradouro = logradouro;
        Municipio = municipio;
        Uf = uf;
        Latitude = latitude;
        Longitude = longitude;
        GeoCodigo = geoCodigo;
    }

    /// <summary>Logradouro/endereço (pode ser vazio em obras lineares sem endereço único).</summary>
    public string Logradouro { get; }

    /// <summary>Município da obra (obrigatório).</summary>
    public string Municipio { get; }

    /// <summary>Unidade Federativa (sigla de 2 letras).</summary>
    public string Uf { get; }

    /// <summary>Latitude (graus decimais), quando georreferenciada.</summary>
    public decimal? Latitude { get; }

    /// <summary>Longitude (graus decimais), quando georreferenciada.</summary>
    public decimal? Longitude { get; }

    /// <summary>Código geográfico/identificador SICOE da localização (opcional).</summary>
    public string? GeoCodigo { get; }

    /// <summary>Cria uma localização de obra validando município/UF.</summary>
    /// <param name="municipio">Município (obrigatório).</param>
    /// <param name="uf">UF (2 letras, obrigatória).</param>
    /// <param name="logradouro">Logradouro (opcional).</param>
    /// <param name="latitude">Latitude (-90 a 90), opcional.</param>
    /// <param name="longitude">Longitude (-180 a 180), opcional.</param>
    /// <param name="geoCodigo">Código geográfico/SICOE, opcional.</param>
    /// <returns>Nova instância de <see cref="LocalizacaoObra"/>.</returns>
    /// <exception cref="ArgumentException">Se município for vazio ou a UF não tiver 2 letras.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se latitude/longitude estiverem fora de faixa.</exception>
    public static LocalizacaoObra Criar(
        string municipio,
        string uf,
        string? logradouro = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string? geoCodigo = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(municipio);
        ArgumentException.ThrowIfNullOrWhiteSpace(uf);
        if (uf.Trim().Length != 2)
        {
            throw new ArgumentException("UF deve ter 2 letras.", nameof(uf));
        }

        if (latitude is { } lat && lat is < -90m or > 90m)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude fora da faixa [-90, 90].");
        }

        if (longitude is { } lon && lon is < -180m or > 180m)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude fora da faixa [-180, 180].");
        }

        return new LocalizacaoObra(
            logradouro?.Trim() ?? string.Empty,
            municipio.Trim(),
            uf.Trim().ToUpperInvariant(),
            latitude,
            longitude,
            string.IsNullOrWhiteSpace(geoCodigo) ? null : geoCodigo.Trim());
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Logradouro;
        yield return Municipio;
        yield return Uf;
        yield return Latitude;
        yield return Longitude;
        yield return GeoCodigo;
    }
}
