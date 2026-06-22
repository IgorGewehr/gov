namespace Tensorroot.Gov.Modules.Educacao.Domain.ValueObjects;

/// <summary>
/// Endereco e localizacao georreferenciada de uma escola. Reune logradouro, municipio, UF
/// e CEP, e as coordenadas (latitude/longitude, base do GeoJSON) usadas na otimizacao e
/// auditoria de rotas de transporte escolar. Objeto de valor imutavel.
/// </summary>
/// <param name="Logradouro">Logradouro (rua, avenida, numero).</param>
/// <param name="Municipio">Municipio.</param>
/// <param name="Uf">Unidade da federacao (sigla de 2 letras).</param>
/// <param name="Cep">CEP (apenas digitos).</param>
/// <param name="Latitude">Latitude da escola (graus decimais).</param>
/// <param name="Longitude">Longitude da escola (graus decimais).</param>
public readonly record struct Endereco(
    string Logradouro,
    string Municipio,
    string Uf,
    string Cep,
    double Latitude,
    double Longitude)
{
    /// <summary>Cria um endereco validado quanto aos campos textuais e ao intervalo das coordenadas.</summary>
    /// <param name="logradouro">Logradouro.</param>
    /// <param name="municipio">Municipio.</param>
    /// <param name="uf">Unidade da federacao (sigla de 2 letras).</param>
    /// <param name="cep">CEP.</param>
    /// <param name="latitude">Latitude (-90 a 90).</param>
    /// <param name="longitude">Longitude (-180 a 180).</param>
    /// <returns>Instancia de <see cref="Endereco"/> normalizada.</returns>
    /// <exception cref="ArgumentException">Se algum campo textual obrigatorio for vazio ou a UF nao tiver 2 letras.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se latitude/longitude estiverem fora do intervalo geografico.</exception>
    public static Endereco Criar(
        string logradouro,
        string municipio,
        string uf,
        string cep,
        double latitude,
        double longitude)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logradouro);
        ArgumentException.ThrowIfNullOrWhiteSpace(municipio);
        ArgumentException.ThrowIfNullOrWhiteSpace(uf);
        ArgumentException.ThrowIfNullOrWhiteSpace(cep);

        var siglaUf = uf.Trim().ToUpperInvariant();
        if (siglaUf.Length != 2)
        {
            throw new ArgumentException("UF deve ter 2 letras.", nameof(uf));
        }

        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude fora do intervalo [-90, 90].");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude fora do intervalo [-180, 180].");
        }

        return new Endereco(
            logradouro.Trim(),
            municipio.Trim(),
            siglaUf,
            cep.Trim(),
            latitude,
            longitude);
    }
}
