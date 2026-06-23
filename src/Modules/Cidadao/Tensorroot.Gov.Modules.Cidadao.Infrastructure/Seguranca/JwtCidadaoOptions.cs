namespace Tensorroot.Gov.Modules.Cidadao.Infrastructure.Seguranca;

/// <summary>
/// Parametros de emissao do JWT do CIDADAO. Le a MESMA secao "Jwt" da configuracao (segredo/issuer/
/// audience identicos ao token interno, para que o JwtBearer do ApiHost valide o token do cidadao sem
/// nova chave). A duracao e propria (sessao do portal pode ser mais curta). Em PRODUCAO o segredo vem
/// do Azure Key Vault — jamais versionado (CLAUDE.md §5).
/// </summary>
public sealed class JwtCidadaoOptions
{
    /// <summary>Nome da secao de configuracao (compartilhada com o token interno).</summary>
    public const string SecaoConfiguracao = "Jwt";

    /// <summary>Segredo simetrico (HS256). Minimo de 32 bytes (256 bits) recomendado.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Emissor (claim "iss").</summary>
    public string Issuer { get; set; } = "tensorroot.gov";

    /// <summary>Audiencia (claim "aud").</summary>
    public string Audience { get; set; } = "tensorroot.gov";

    /// <summary>Tempo de vida do token do cidadao, em minutos (sessao do portal).</summary>
    public int DuracaoMinutosCidadao { get; set; } = 60;
}
