using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tensorroot.Gov.IntegrationTests.Infrastructure;
using Xunit;

namespace Tensorroot.Gov.IntegrationTests;

/// <summary>
/// Smoke test do harness WireMock.Net IN-PROCESS (ambiente SEM Docker). Garante que o stub dos externos
/// (PNCP/eSocial/TCE) sobe e responde — a rede de protecao que W9.1 (PNCP), W9.4 (TSP/ACT) e W9.6
/// (Transferegov) vao REUSAR ao introduzir seus gateways/ACL. Hoje nao ha cliente HTTP real chamando
/// estes endpoints; o stub e exercido por um HttpClient direto. // TODO M9 W9.1: apontar o gateway PNCP
/// para StubExternosWireMock.UrlBase via configuracao e remover este HttpClient direto.
/// </summary>
public sealed class StubExternosWireMockTests
{
    [Fact]
    public async Task Stub_PNCP_responde_login_e_publicacao_de_contrato()
    {
        using var stub = new StubExternosWireMock();
        using var http = new HttpClient { BaseAddress = new Uri(stub.UrlBase) };

        var login = await http.PostAsJsonAsync("/pncp/v1/usuarios/login", new { login = "x", senha = "y" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var publicacao = await http.PostAsJsonAsync("/pncp/v1/orgaos/07000000000001/contratos", new { contrato = "..." });
        publicacao.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await publicacao.Content.ReadAsStringAsync();
        corpo.Should().Contain("numeroControlePNCP");
    }

    [Fact]
    public async Task Stub_eSocial_e_Tce_respondem_recepcao_de_lote_e_remessa()
    {
        using var stub = new StubExternosWireMock();
        using var http = new HttpClient { BaseAddress = new Uri(stub.UrlBase) };

        (await http.PostAsJsonAsync("/esocial/empregador/lotes/eventos", new { lote = "..." }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await http.PostAsJsonAsync("/tce-rs/remessas", new { remessa = "..." }))
            .StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
