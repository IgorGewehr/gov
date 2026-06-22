namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Seguranca;

/// <summary>
/// Parametros de emissao/validacao do JWT auto-emitido (secao "Jwt" da configuracao). Em PRODUCAO
/// o segredo DEVE vir do Azure Key Vault — jamais versionado no repositorio.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Nome da secao de configuracao.</summary>
    public const string SecaoConfiguracao = "Jwt";

    /// <summary>Segredo simetrico (HS256). Minimo de 32 bytes (256 bits) recomendado.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Emissor (claim "iss").</summary>
    public string Issuer { get; set; } = "tensorroot.gov";

    /// <summary>Audiencia (claim "aud").</summary>
    public string Audience { get; set; } = "tensorroot.gov";

    /// <summary>Tempo de vida do token, em minutos.</summary>
    public int DuracaoMinutos { get; set; } = 60;
}
