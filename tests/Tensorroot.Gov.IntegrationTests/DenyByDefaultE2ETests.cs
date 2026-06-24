using System.Net;
using FluentAssertions;
using Tensorroot.Gov.IntegrationTests.Infrastructure;
using Tensorroot.Gov.Modules.Identidade.Domain.Permissoes;
using Tensorroot.Gov.Platform.Tenancy;
using Xunit;

namespace Tensorroot.Gov.IntegrationTests;

/// <summary>
/// E2E de ALTO VALOR (W9.8) — DENY-BY-DEFAULT (negar por padrao) ponta-a-ponta:
/// <list type="bullet">
///   <item><b>401</b>: endpoint protegido SEM token -> a FallbackPolicy global (RequireAuthenticatedUser
///         no Program.cs) FALHA FECHADO. Esquecer a anotacao num endpoint novo nao o expoe.</item>
///   <item><b>403</b>: token VALIDO porem SEM a permissao "perm" exigida -> o handler RBAC nega (a
///         ausencia da claim = sem acesso).</item>
///   <item><b>200</b>: o MESMO endpoint, com a permissao correta, responde — prova que o 401/403 vem da
///         autorizacao, nao de erro de rota.</item>
/// </list>
/// As superficies legitimamente anonimas (health, raiz) continuam abertas (AllowAnonymous tem precedencia).
/// </summary>
[Collection(ApiHostCollection.Nome)]
public sealed class DenyByDefaultE2ETests(ApiHostFixture fixture)
{
    private readonly CustomWebApplicationFactory _factory = fixture.Factory;

    // Endpoints protegidos representativos de modulos distintos (leitura), todos exigem uma "perm".
    public static TheoryData<string> EndpointsProtegidos => new()
    {
        "/api/administracao/contratos/vigentes?referencia=2026-01-01",
        "/api/saude/estabelecimentos",
        "/api/patrimonio/bens",
    };

    [Theory]
    [MemberData(nameof(EndpointsProtegidos))]
    public async Task Sem_token_endpoint_protegido_responde_401(string url)
    {
        // Cliente SEM Authorization header.
        var clienteAnonimo = _factory.CreateClient();

        var resposta = await clienteAnonimo.GetAsync(url);

        resposta.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "a FallbackPolicy deny-by-default exige usuario autenticado em endpoint sem AllowAnonymous");
    }

    [Fact]
    public async Task Com_token_sem_a_permissao_exigida_responde_403()
    {
        var tenant = await _factory.ProvisionarTenantAsync("16.345.678/0001-46", "Municipio Deny/RS", PoderTenant.Executivo);

        // Token VALIDO (assinatura/issuer/audience corretos, tenant resolvido) porem com permissao IRRELEVANTE
        // para o endpoint (so "saude.ver"), provando que a negacao vem do RBAC, nao da autenticacao.
        var tokenSemPermissao = EmissorTokenDeTeste.Emitir(tenant, new[] { Permissoes.SaudeVer });
        var cliente = _factory.CreateClient().ComToken(tokenSemPermissao);

        // Exige "administracao.ver" — ausente no token.
        var resposta = await cliente.GetAsync("/api/administracao/contratos/vigentes?referencia=2026-01-01");

        resposta.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "token autenticado sem a claim 'perm' exigida deve ser negado (403), nao 401");
    }

    [Fact]
    public async Task Com_a_permissao_correta_o_mesmo_endpoint_responde_200()
    {
        var tenant = await _factory.ProvisionarTenantAsync("17.345.678/0001-09", "Municipio Allow/RS", PoderTenant.Executivo);

        var tokenComPermissao = EmissorTokenDeTeste.Emitir(tenant, new[] { Permissoes.AdministracaoVer });
        var cliente = _factory.CreateClient().ComToken(tokenComPermissao);

        var resposta = await cliente.GetAsync("/api/administracao/contratos/vigentes?referencia=2026-01-01");

        resposta.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "com a permissao exata o endpoint autoriza — confirma que 401/403 anteriores vieram da autorizacao");
    }

    [Fact]
    public async Task Superficies_anonimas_permanecem_abertas()
    {
        var clienteAnonimo = _factory.CreateClient();

        // Health e raiz sao marcados AllowAnonymous (precedencia sobre a FallbackPolicy).
        (await clienteAnonimo.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await clienteAnonimo.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
