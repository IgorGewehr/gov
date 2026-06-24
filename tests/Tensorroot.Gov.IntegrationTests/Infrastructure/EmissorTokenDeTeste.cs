using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Tensorroot.Gov.IntegrationTests.Infrastructure;

/// <summary>
/// Emissor de JWT de TESTE. Produz um token HS256 REAL — assinado com o mesmo segredo
/// (<see cref="CustomWebApplicationFactory.JwtSecret"/>) e com os MESMOS nomes de claim do
/// <c>EmissorToken</c> de producao ("tenant_id", "perm", sub/name/email) — de modo que o pipeline de
/// AuthN/AuthZ do ApiHost o valide exatamente como validaria um token emitido pelo modulo Identidade.
///
/// Nao reusamos o login por senha de proposito: o harness controla DIRETAMENTE o tenant e o conjunto de
/// permissoes, permitindo provar deny-by-default (token sem a permissao -> 403) e isolamento cross-tenant
/// (token do tenant A nunca enxerga dado do tenant B) sem depender do estado de usuarios semeados.
/// </summary>
public static class EmissorTokenDeTeste
{
    /// <summary>Tipo de claim do tenant (espelha EmissorToken.ClaimTenantId).</summary>
    public const string ClaimTenantId = "tenant_id";

    /// <summary>Tipo de claim de permissao RBAC (espelha EmissorToken.ClaimPermissao).</summary>
    public const string ClaimPermissao = "perm";

    /// <summary>
    /// Emite um JWT para o tenant informado com EXATAMENTE as permissoes fornecidas (deny-by-default:
    /// a ausencia de uma claim "perm" = sem o acesso correspondente).
    /// </summary>
    /// <param name="tenantId">Tenant dono do token (claim "tenant_id").</param>
    /// <param name="permissoes">Permissoes efetivas (uma claim "perm" por item).</param>
    /// <param name="usuarioId">Subject (opcional; gerado se ausente).</param>
    /// <returns>O JWT serializado (sem o prefixo "Bearer ").</returns>
    public static string Emitir(Guid tenantId, IEnumerable<string> permissoes, Guid? usuarioId = null)
    {
        ArgumentNullException.ThrowIfNull(permissoes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, (usuarioId ?? Guid.NewGuid()).ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Name, "Usuario de Teste"),
            new(JwtRegisteredClaimNames.Email, "teste@tensorroot.gov"),
            new(ClaimTenantId, tenantId.ToString()),
        };

        foreach (var permissao in permissoes)
        {
            if (!string.IsNullOrWhiteSpace(permissao))
            {
                claims.Add(new Claim(ClaimPermissao, permissao));
            }
        }

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(CustomWebApplicationFactory.JwtSecret));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);
        var agora = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: CustomWebApplicationFactory.JwtIssuer,
            audience: CustomWebApplicationFactory.JwtAudience,
            claims: claims,
            notBefore: agora,
            expires: agora.AddMinutes(60),
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Token com TODAS as permissoes de tenant (papel "Administrador") — atalho para cenarios felizes.</summary>
    /// <param name="tenantId">Tenant dono do token.</param>
    /// <returns>JWT com Permissoes.Todas.</returns>
    public static string EmitirAdministrador(Guid tenantId)
        => Emitir(tenantId, Tensorroot.Gov.Modules.Identidade.Domain.Permissoes.Permissoes.Todas);
}
