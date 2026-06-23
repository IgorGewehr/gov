using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;
using Tensorroot.Gov.Modules.Cidadao.Domain.Contas;

namespace Tensorroot.Gov.Modules.Cidadao.Infrastructure.Seguranca;

/// <summary>
/// Emite JWTs do CIDADAO assinados em HS256 com o MESMO segredo da configuracao (o JwtBearer do ApiHost
/// valida sem nova chave). Embute o subject (conta-cidadao), o tenant e a claim discriminante
/// <c>tipo=cidadao</c> — e DELIBERADAMENTE NENHUMA claim "perm" (o cidadao nao tem papel no RBAC
/// organizacional; o gating do portal e por <c>tipo=cidadao</c>, nao por permissao). SEGURANCA CRITICA:
/// o segredo nunca e logado e provem do Key Vault em producao.
/// </summary>
public sealed class EmissorTokenCidadao(IOptions<JwtCidadaoOptions> opcoes, TimeProvider timeProvider) : IEmissorTokenCidadao
{
    /// <summary>Tipo de claim discriminante do realm externo (valor fixo <see cref="ValorTipoCidadao"/>).</summary>
    public const string ClaimTipo = "tipo";

    /// <summary>Valor da claim <see cref="ClaimTipo"/> para o principal cidadao.</summary>
    public const string ValorTipoCidadao = "cidadao";

    /// <summary>Tipo de claim do identificador do tenant (compativel com o token interno).</summary>
    public const string ClaimTenantId = "tenant_id";

    /// <summary>Tipo de claim do documento (CPF/CNPJ) do cidadao.</summary>
    public const string ClaimDocumento = "documento";

    /// <summary>Tipo de claim do selo gov.br, quando houver.</summary>
    public const string ClaimSelo = "selo";

    private readonly JwtCidadaoOptions _opcoes = (opcoes ?? throw new ArgumentNullException(nameof(opcoes))).Value;

    /// <inheritdoc />
    public TokenCidadaoEmitido Emitir(
        CidadaoContaId contaId,
        Guid tenantId,
        string nome,
        string documento,
        SeloGovBr? selo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(documento);

        if (string.IsNullOrWhiteSpace(_opcoes.Secret))
        {
            throw new InvalidOperationException("Jwt:Secret nao configurado — emissao de token do cidadao bloqueada.");
        }

        var agora = timeProvider.GetUtcNow().UtcDateTime;
        var expiraEm = agora.AddMinutes(_opcoes.DuracaoMinutosCidadao);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, contaId.Value.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Name, nome),
            new(ClaimTenantId, tenantId.ToString()),
            new(ClaimTipo, ValorTipoCidadao),
            new(ClaimDocumento, documento),
        };

        if (selo is not null)
        {
            claims.Add(new Claim(ClaimSelo, selo.Value.ToString()));
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
        return new TokenCidadaoEmitido(jwt, expiraEm);
    }
}
