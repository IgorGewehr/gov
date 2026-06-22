using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Infrastructure.Seguranca;
using Xunit;

namespace Tensorroot.Gov.Modules.Identidade.Tests;

/// <summary>
/// Cobertura da emissao e validacao do JWT (HS256): claims corretas (subject, tenant, nome, e-mail
/// e uma claim "perm" por permissao efetiva), assinatura/issuer/audience validos e recusa de
/// segredo ausente. E SEGURANCA CRITICA: token sem segredo nao pode ser emitido.
/// </summary>
public sealed class EmissorTokenTests
{
    private const string Segredo = "segredo-de-teste-super-secreto-com-mais-de-32-bytes-1234567890";

    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static EmissorToken CriarEmissor(JwtOptions? opcoes = null)
        => new(
            Options.Create(opcoes ?? new JwtOptions
            {
                Secret = Segredo,
                Issuer = "tensorroot.gov",
                Audience = "tensorroot.gov",
                DuracaoMinutos = 60,
            }),
            TimeProvider.System);

    private static JwtSecurityToken Validar(string jwt, JwtOptions opcoes)
    {
        var parametros = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = opcoes.Issuer,
            ValidateAudience = true,
            ValidAudience = opcoes.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcoes.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        new JwtSecurityTokenHandler().ValidateToken(jwt, parametros, out var validado);
        return (JwtSecurityToken)validado;
    }

    [Fact]
    public void Emitir_produz_token_validavel_com_as_claims_corretas()
    {
        var opcoes = new JwtOptions { Secret = Segredo, Issuer = "tensorroot.gov", Audience = "tensorroot.gov", DuracaoMinutos = 60 };
        var emissor = CriarEmissor(opcoes);
        var usuarioId = UsuarioId.New();
        string[] permissoes = ["tributos.ver", "tributos.gerenciar"];

        var emitido = emissor.Emitir(usuarioId, TenantA, "Prefeitura X", "Maria", "maria@x.gov.br", permissoes);

        emitido.AccessToken.Should().NotBeNullOrWhiteSpace();
        emitido.ExpiraEm.Should().BeAfter(DateTimeOffset.UtcNow);

        var token = Validar(emitido.AccessToken, opcoes);
        token.Issuer.Should().Be("tensorroot.gov");
        token.Audiences.Should().Contain("tensorroot.gov");

        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == usuarioId.Value.ToString());
        token.Claims.Should().Contain(c => c.Type == EmissorToken.ClaimTenantId && c.Value == TenantA.ToString());
        token.Claims.Should().Contain(c => c.Type == EmissorToken.ClaimTenantNome && c.Value == "Prefeitura X");

        var permsNoToken = token.Claims.Where(c => c.Type == EmissorToken.ClaimPermissao).Select(c => c.Value).ToList();
        permsNoToken.Should().BeEquivalentTo(permissoes);
    }

    [Fact]
    public void Emitir_inclui_nome_e_email_do_usuario()
    {
        var opcoes = new JwtOptions { Secret = Segredo, Issuer = "tensorroot.gov", Audience = "tensorroot.gov", DuracaoMinutos = 60 };
        var emissor = CriarEmissor(opcoes);

        var emitido = emissor.Emitir(UsuarioId.New(), TenantA, null, "Joao", "joao@x.gov.br", []);

        var token = Validar(emitido.AccessToken, opcoes);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Name && c.Value == "Joao");
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "joao@x.gov.br");
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void Emitir_sem_permissoes_nao_gera_claim_perm()
    {
        var opcoes = new JwtOptions { Secret = Segredo, Issuer = "tensorroot.gov", Audience = "tensorroot.gov", DuracaoMinutos = 60 };
        var emissor = CriarEmissor(opcoes);

        var emitido = emissor.Emitir(UsuarioId.New(), TenantA, null, "Sem", "sem@x.gov.br", []);

        var token = Validar(emitido.AccessToken, opcoes);
        token.Claims.Should().NotContain(c => c.Type == EmissorToken.ClaimPermissao);
    }

    [Fact]
    public void Emitir_sem_segredo_configurado_bloqueia_a_emissao()
    {
        var emissor = CriarEmissor(new JwtOptions { Secret = string.Empty, DuracaoMinutos = 60 });

        var acao = () => emissor.Emitir(UsuarioId.New(), TenantA, null, "X", "x@x.gov.br", []);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Token_assinado_com_segredo_diferente_nao_valida()
    {
        var opcoes = new JwtOptions { Secret = Segredo, Issuer = "tensorroot.gov", Audience = "tensorroot.gov", DuracaoMinutos = 60 };
        var emissor = CriarEmissor(opcoes);
        var emitido = emissor.Emitir(UsuarioId.New(), TenantA, null, "X", "x@x.gov.br", []);

        var outroSegredo = new JwtOptions
        {
            Secret = "OUTRO-segredo-completamente-diferente-com-mais-de-32-bytes-000",
            Issuer = "tensorroot.gov",
            Audience = "tensorroot.gov",
            DuracaoMinutos = 60,
        };

        var acao = () => Validar(emitido.AccessToken, outroSegredo);

        acao.Should().Throw<SecurityTokenSignatureKeyNotFoundException>();
    }
}
