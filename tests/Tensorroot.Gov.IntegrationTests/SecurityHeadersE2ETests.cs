using FluentAssertions;
using Tensorroot.Gov.IntegrationTests.Infrastructure;
using Xunit;

namespace Tensorroot.Gov.IntegrationTests;

/// <summary>
/// E2E (P8) — CABECALHOS DE SEGURANCA: toda resposta, mesmo a anonima de /health, carrega os headers
/// de hardening INVARIANTES de ambiente (nosniff, X-Frame-Options DENY, Referrer-Policy,
/// X-Permitted-Cross-Domain-Policies). HSTS e CSP sao emitidos APENAS fora de Development — o host de
/// teste roda em Development, logo nao sao exigidos aqui (cobertos por revisao/operacao de go-live).
/// </summary>
[Collection(ApiHostCollection.Nome)]
public sealed class SecurityHeadersE2ETests(ApiHostFixture fixture)
{
    private readonly CustomWebApplicationFactory _factory = fixture.Factory;

    [Theory]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("Referrer-Policy", "no-referrer")]
    [InlineData("X-Permitted-Cross-Domain-Policies", "none")]
    public async Task Resposta_carrega_cabecalhos_de_seguranca_invariantes(string header, string valorEsperado)
    {
        var clienteAnonimo = _factory.CreateClient();

        var resposta = await clienteAnonimo.GetAsync("/health");

        resposta.Headers.TryGetValues(header, out var valores).Should().BeTrue(
            $"toda resposta deve emitir {header} (hardening de borda P8)");
        valores!.Should().ContainSingle().Which.Should().Be(valorEsperado);
    }
}
