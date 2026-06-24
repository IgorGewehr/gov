using System.Diagnostics;
using System.Net;
using FluentAssertions;
using Tensorroot.Gov.IntegrationTests.Infrastructure;
using Tensorroot.Gov.Platform.Tenancy;
using Xunit;

namespace Tensorroot.Gov.IntegrationTests;

/// <summary>
/// E2E da ROBUSTEZ residual (M9 W9.7) — prova do ENRICHER multi-tenant de observabilidade.
///
/// Invariante sob teste: toda requisicao autenticada produz um SINAL (span de trace) que carrega a
/// dimensao <c>tenant.id</c> = o tenant resolvido do JWT. Sem isso, os traces das integracoes
/// governamentais ficariam "orfaos" de tenant no backend (impossivel filtrar/alarmar por ente publico).
///
/// O enricher e a dupla <c>ContextoCorrelacaoMiddleware</c> (semeia tenant.id no span raiz e no Baggage,
/// a partir do <c>ITenantContext</c> do JWT) + <c>EnriquecedorTenantSpanProcessor</c> (propaga do Baggage
/// para todo span filho). Capturamos o span REAL da requisicao com um <see cref="ActivityListener"/> do
/// BCL (sem novos pacotes) e provamos que a tag <c>tenant.id</c> esta presente e igual ao tenant do token.
///
/// O <c>tenant.id</c> e ECOADO no <c>X-Correlation-Id</c> da resposta tambem fecha o rastro ponta-a-ponta,
/// mas a prova central aqui e a DIMENSAO no span (o sinal de observabilidade).
/// </summary>
[Collection(ApiHostCollection.Nome)]
public sealed class EnricherTenantObservabilidadeE2ETests(ApiHostFixture fixture)
{
    private const string TenantIdKey = "tenant.id";

    private readonly CustomWebApplicationFactory _factory = fixture.Factory;

    [Fact]
    public async Task Requisicao_autenticada_emite_span_com_tag_tenant_id_do_jwt()
    {
        // Arrange: tenant proprio + token de administrador (carrega o tenant nas claims do JWT).
        var tenant = await _factory.ProvisionarTenantAsync(
            "31.345.678/0001-30", "Prefeitura E2E Enricher/RS", PoderTenant.Executivo);
        var cliente = _factory.CreateClient().ComToken(EmissorTokenDeTeste.EmitirAdministrador(tenant));

        // Captura TODOS os spans (sample tudo) e guarda os que carregam tenant.id ao FINALIZAR — momento em
        // que o middleware ja semeou a tag no span raiz da requisicao a partir do ITenantContext do JWT.
        var tenantIdsCapturados = new List<string>();
        var trava = new object();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = atividade =>
            {
                var valor = atividade.GetTagItem(TenantIdKey) as string;
                if (!string.IsNullOrEmpty(valor))
                {
                    lock (trava)
                    {
                        tenantIdsCapturados.Add(valor);
                    }
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        // Act: uma requisicao autenticada real (rota existente e autorizada para o administrador).
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var resposta = await cliente.GetAsync($"/api/administracao/contratos/vigentes?referencia={hoje}");

        // Sanidade: a rota respondeu (autenticada). O foco do teste e o SINAL, nao o corpo.
        resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());

        // Assert: ao menos um span da requisicao carregou tenant.id, e SEMPRE igual ao tenant do JWT —
        // o enricher nunca vaza/identifica outro tenant no sinal.
        lock (trava)
        {
            tenantIdsCapturados.Should().NotBeEmpty(
                "o ContextoCorrelacaoMiddleware semeia tenant.id no span da requisicao autenticada");
            tenantIdsCapturados.Should().OnlyContain(
                id => id == tenant.ToString(),
                "o enricher marca o sinal com EXATAMENTE o tenant resolvido do JWT (isolamento multi-tenant)");
        }
    }
}
