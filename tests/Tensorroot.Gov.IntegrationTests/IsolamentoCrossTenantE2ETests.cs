using System.Net;
using System.Text.Json;
using FluentAssertions;
using Tensorroot.Gov.IntegrationTests.Infrastructure;
using Tensorroot.Gov.Platform.Tenancy;
using Xunit;

namespace Tensorroot.Gov.IntegrationTests;

/// <summary>
/// E2E de ALTO VALOR (W9.8) — ISOLAMENTO multi-tenant PONTA-A-PONTA: o tenant A NUNCA enxerga dado do
/// tenant B (e vice-versa), em >= 3 modulos distintos, atravessando todo o pipeline real (JWT ->
/// ITenantContext -> Global Query Filter -> banco DEDICADO por tenant).
///
/// Cada tenant tem seu PROPRIO banco SQLite (database-per-tenant). A prova combina dois angulos:
/// <list type="bullet">
///   <item>POSITIVO: A cria um recurso e SO A o ve (lista/po-id de A retorna o dado).</item>
///   <item>NEGATIVO: B, com token valido do PROPRIO tenant, ve LISTA VAZIA / 404 para o mesmo recurso.</item>
/// </list>
/// Vazamento entre tenants e falha CRITICA (CLAUDE.md §3/§5) — este teste e a rede que o impede.
/// </summary>
[Collection(ApiHostCollection.Nome)]
public sealed class IsolamentoCrossTenantE2ETests(ApiHostFixture fixture)
{
    private readonly CustomWebApplicationFactory _factory = fixture.Factory;

    [Fact]
    public async Task TenantA_nao_enxerga_dado_de_TenantB_em_administracao_saude_e_patrimonio()
    {
        // Dois tenants distintos, cada um com banco dedicado e token de administrador proprio.
        var tenantA = await _factory.ProvisionarTenantAsync("14.345.678/0001-10", "Municipio A/RS", PoderTenant.Executivo);
        var tenantB = await _factory.ProvisionarTenantAsync("15.345.678/0001-83", "Municipio B/RS", PoderTenant.Executivo);

        var clienteA = _factory.CreateClient().ComToken(EmissorTokenDeTeste.EmitirAdministrador(tenantA));
        var clienteB = _factory.CreateClient().ComToken(EmissorTokenDeTeste.EmitirAdministrador(tenantB));

        // ===================== MODULO 1 — ADMINISTRACAO (fornecedor) =====================
        var respFornecedor = await clienteA.PostJsonAsync("/api/administracao/fornecedores", new
        {
            Cnpj = "33.333.333/0001-91",
            RazaoSocial = "Fornecedora do Municipio A",
        });
        respFornecedor.StatusCode.Should().Be(HttpStatusCode.OK, await respFornecedor.Content.ReadAsStringAsync());
        var fornecedorId = (await respFornecedor.LerJsonAsync()).GetProperty("id").GetGuid();

        // A enxerga o proprio fornecedor (200); B recebe 404 para o MESMO id (filtro de tenant).
        (await clienteA.GetAsync($"/api/administracao/fornecedores/{fornecedorId}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await clienteB.GetAsync($"/api/administracao/fornecedores/{fornecedorId}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound, "B nao pode ver o fornecedor cadastrado por A");

        // ===================== MODULO 2 — SAUDE (estabelecimento) =====================
        var respEstab = await clienteA.PostJsonAsync("/api/saude/estabelecimentos", new
        {
            Cnes = "1234567",
            Nome = "UBS Central do Municipio A",
            Tipo = "Ubs",
            Endereco = new
            {
                Logradouro = "Rua Central",
                Numero = "100",
                Bairro = "Centro",
                Municipio = "Municipio A",
                Uf = "RS",
                Cep = "99900000",
            },
        });
        respEstab.StatusCode.Should().Be(HttpStatusCode.OK, await respEstab.Content.ReadAsStringAsync());

        // A lista >= 1 estabelecimento; B lista ZERO.
        ContarTotal(await (await clienteA.GetAsync("/api/saude/estabelecimentos")).LerJsonAsync())
            .Should().BeGreaterThanOrEqualTo(1);
        ContarTotal(await (await clienteB.GetAsync("/api/saude/estabelecimentos")).LerJsonAsync())
            .Should().Be(0, "B nao pode ver estabelecimentos cadastrados por A");

        // ===================== MODULO 3 — PATRIMONIO (bem) =====================
        var respBem = await clienteA.PostJsonAsync("/api/patrimonio/bens", new
        {
            Descricao = "Notebook do Municipio A",
            Tipo = 1, // Movel
            ValorInicial = 4_000m,
            ValorResidual = 400m,
            VidaUtilMeses = 60,
            DataIncorporacao = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            Origem = "Aquisicao direta",
        });
        respBem.StatusCode.Should().Be(HttpStatusCode.OK, await respBem.Content.ReadAsStringAsync());

        ContarTotal(await (await clienteA.GetAsync("/api/patrimonio/bens")).LerJsonAsync())
            .Should().BeGreaterThanOrEqualTo(1);
        ContarTotal(await (await clienteB.GetAsync("/api/patrimonio/bens")).LerJsonAsync())
            .Should().Be(0, "B nao pode ver bens incorporados por A");
    }

    /// <summary>
    /// Le o total de uma resposta de listagem, seja ela um <c>ResultadoPaginado</c> (objeto com "total")
    /// ou um array JSON puro. Tolerante a divergencias de envelope entre modulos.
    /// </summary>
    private static int ContarTotal(JsonElement corpo)
    {
        if (corpo.ValueKind == JsonValueKind.Array)
        {
            return corpo.GetArrayLength();
        }

        if (corpo.ValueKind == JsonValueKind.Object && corpo.TryGetProperty("total", out var total))
        {
            return total.GetInt32();
        }

        if (corpo.ValueKind == JsonValueKind.Object && corpo.TryGetProperty("itens", out var itens) && itens.ValueKind == JsonValueKind.Array)
        {
            return itens.GetArrayLength();
        }

        return 0;
    }
}
