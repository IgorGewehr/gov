using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Seguranca;

/// <summary>
/// Emite JWTs assinados em HS256 com o segredo simetrico de configuracao. Embute o subject
/// (usuario), tenant, nome/e-mail e UMA claim "perm" por permissao efetiva (alem de "role" quando
/// aplicavel). E SEGURANCA CRITICA: o segredo nunca e logado e provem do Key Vault em producao.
/// </summary>
public sealed class EmissorToken(IOptions<JwtOptions> opcoes, TimeProvider timeProvider) : IEmissorToken
{
    /// <summary>Tipo de claim para uma permissao RBAC efetiva.</summary>
    public const string ClaimPermissao = "perm";

    /// <summary>Tipo de claim para o identificador do tenant.</summary>
    public const string ClaimTenantId = "tenant_id";

    /// <summary>Tipo de claim para o nome do tenant.</summary>
    public const string ClaimTenantNome = "tenant_name";

    /// <summary>Tipo de claim para um papel (role) RBAC.</summary>
    public const string ClaimRole = "role";

    private readonly JwtOptions _opcoes = (opcoes ?? throw new ArgumentNullException(nameof(opcoes))).Value;

    /// <inheritdoc />
    public TokenEmitido Emitir(
        UsuarioId usuarioId,
        Guid tenantId,
        string? tenantNome,
        string nome,
        string email,
        IReadOnlyCollection<string> permissoesEfetivas)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentNullException.ThrowIfNull(permissoesEfetivas);

        if (string.IsNullOrWhiteSpace(_opcoes.Secret))
        {
            throw new InvalidOperationException("Jwt:Secret nao configurado — emissao de token bloqueada.");
        }

        var agora = timeProvider.GetUtcNow().UtcDateTime;
        var expiraEm = agora.AddMinutes(_opcoes.DuracaoMinutos);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuarioId.Value.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Name, nome),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTenantId, tenantId.ToString()),
        };

        if (!string.IsNullOrWhiteSpace(tenantNome))
        {
            claims.Add(new Claim(ClaimTenantNome, tenantNome));
        }

        // Uma claim "perm" por permissao efetiva (negar por padrao: ausencia = sem acesso).
        foreach (var permissao in permissoesEfetivas)
        {
            if (!string.IsNullOrWhiteSpace(permissao))
            {
                claims.Add(new Claim(ClaimPermissao, permissao));
            }
        }

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opcoes.Secret));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opcoes.Issuer,
            audience: _opcoes.Audience,
            claims: claims,
            notBefore: agora,
            expires: expiraEm,
            signingCredentials: credenciais);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenEmitido(jwt, expiraEm);
    }
}
