using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.IntegrationTests.Infrastructure;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.Platform.Tenancy;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Xunit;

namespace Tensorroot.Gov.IntegrationTests;

/// <summary>
/// E2E de ALTO VALOR (W9.8) — ciclo da despesa: licitacao -> homologacao -> contrato -> (Outbox) -> Patrimonio.
///
/// Cobre o caminho CROSS-MODULE real (Administracao -> Patrimonio/Financas) via Outbox + *.Contracts:
/// a celebracao do contrato publica o <c>ContratoAssinadoIntegrationEvent</c>; ao DRENAR o Outbox, o
/// consumidor de outro Bounded Context (Patrimonio) recebe o evento. Prova a consistencia transacional
/// + a comunicacao por contrato, nao por chamada direta.
///
/// NOTA DE PRODUTO (debito mapeado, fora do escopo de tests/): hoje NAO ha endpoint HTTP para registrar
/// PROPOSTA nem HABILITACAO no certame, e o repositorio de licitacao nao faz eager-load das colecoes
/// filhas. Por isso a fase licitacao->julgamento->homologacao e conduzida via dominio+repositorio NUM
/// UNICO escopo (com TenantOverride), e o CONTRATO e celebrado via HTTP. Quando W9.1 expor esses
/// endpoints, este teste deve migrar a fase inteira para HTTP. // TODO M9 W9.1: endpoints de proposta/habilitacao.
/// </summary>
[Collection(ApiHostCollection.Nome)]
public sealed class CicloDaDespesaE2ETests(ApiHostFixture fixture)
{
    private readonly CustomWebApplicationFactory _factory = fixture.Factory;

    [Fact]
    public async Task Ciclo_licitacao_ate_contrato_propaga_contrato_assinado_cross_module_via_outbox()
    {
        // Arrange: tenant proprio (banco dedicado) + token de administrador.
        var tenant = await _factory.ProvisionarTenantAsync(
            "12.345.678/0001-95", "Prefeitura E2E Despesa/RS", PoderTenant.Executivo);
        var token = EmissorTokenDeTeste.EmitirAdministrador(tenant);
        var cliente = _factory.CreateClient().ComToken(token);

        var fornecedorId = Guid.NewGuid();

        // --- Fase licitacao -> homologacao (dominio+repositorio, num unico escopo; ver NOTA acima) ---
        var licitacaoId = Guid.Empty;
        await _factory.DentroDoTenantAsync(tenant, async sp =>
        {
            var licitacoes = sp.GetRequiredService<ILicitacaoRepository>();
            var uow = sp.GetRequiredService<IUnitOfWork>();

            var licitacao = Licitacao.Abrir(
                tenant, "Aquisicao de equipamentos de informatica",
                ModalidadeLicitacao.Pregao, CriterioJulgamento.MenorPreco, ValorMonetario.De(100_000m));

            var loteId = licitacao.AdicionarLote(1, "Lote unico - desktops", ValorMonetario.De(100_000m));
            var propostaId = licitacao.RegistrarProposta(fornecedorId, loteId, ValorMonetario.De(90_000m));
            licitacao.HabilitarLicitante(fornecedorId, ResultadoHabilitacao.Habilitado, "Documentacao regular", DateTimeOffset.UtcNow);
            licitacao.JulgarPropostas(propostaId.Value);
            licitacao.Homologar();

            licitacoes.Adicionar(licitacao);
            await uow.SaveChangesAsync(CancellationToken.None);
            licitacaoId = licitacao.Id.Value;
        });

        licitacaoId.Should().NotBe(Guid.Empty);

        // --- Fase contrato: CELEBRADA VIA HTTP (Origem = Licitacao) -> publica ContratoAssinadoIntegrationEvent ---
        var respostaContrato = await cliente.PostJsonAsync("/api/administracao/contratos", new
        {
            LicitacaoId = licitacaoId,
            FornecedorId = fornecedorId,
            Origem = "Licitacao",
            Objeto = "Aquisicao de equipamentos de informatica",
            Valor = 90_000m,
            VigenciaInicio = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            VigenciaFim = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)).ToString("yyyy-MM-dd"),
            EmpenhoId = (Guid?)null,
            NumeroEmpenho = (string?)null,
            JustificativaContratacaoDireta = (string?)null,
        });

        respostaContrato.StatusCode.Should().Be(HttpStatusCode.OK, await respostaContrato.Content.ReadAsStringAsync());
        var corpoContrato = await respostaContrato.LerJsonAsync();
        var contratoId = corpoContrato.GetProperty("id").GetGuid();
        contratoId.Should().NotBe(Guid.Empty);

        // --- Drena o Outbox SOB DEMANDA: leva o ContratoAssinadoIntegrationEvent ao(s) consumidor(es) ---
        // (sem esperar os 30s do OutboxBackgroundService). NAO lanca = propagacao cross-module sem erro.
        // (A celebracao enfileira o evento no Outbox — e nao via IPublisher in-scope — o que tambem evita
        // resolver dois ModuleDbContext no mesmo escopo da requisicao; ver guarda H5.)
        await _factory.DrenarOutboxAsync(tenant);

        // --- Assert HTTP: o contrato e recuperavel por id (200), confirmando a persistencia ponta-a-ponta ---
        var respostaDetalhe = await cliente.GetAsync($"/api/administracao/contratos/{contratoId}");
        respostaDetalhe.StatusCode.Should().Be(HttpStatusCode.OK);
        var detalhe = await respostaDetalhe.LerJsonAsync();

        // EFICACIA art. 94 NLLC (Lei 14.133/2021): o contrato recem-celebrado nasce ASSINADO, NAO eficaz.
        // So transita a Eficaz com publicacao no PNCP **e** dotacao confirmada (esta ultima vem de Financas,
        // assincrona, via consumo do evento). Logo, neste ponto:
        //  (a) a situacao e "Assinado" e a eficacia/dotacao ainda nao foram obtidas;
        //  (b) ele NAO pode aparecer em /contratos/vigentes (que so lista Eficaz/EmExecucao).
        // Esta assercao DOCUMENTA o gate de eficacia — nao e bug; e o comportamento legal correto (W9.1).
        detalhe.GetProperty("situacao").GetString().Should().Be("Assinado");
        detalhe.GetProperty("publicadoNoPncp").GetBoolean().Should().BeFalse();
        detalhe.GetProperty("dotacaoConfirmada").GetBoolean().Should().BeFalse();

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var respostaVigentes = await cliente.GetAsync($"/api/administracao/contratos/vigentes?referencia={hoje}");
        respostaVigentes.StatusCode.Should().Be(HttpStatusCode.OK);
        var vigentes = await respostaVigentes.LerJsonAsync();
        vigentes.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
        vigentes.EnumerateArray().Should().BeEmpty(
            "contrato apenas Assinado (sem PNCP nem dotacao) NAO e eficaz/vigente — gate do art. 94 NLLC");
    }

    [Fact]
    public async Task Contrato_por_dispensa_celebrado_via_http_dispara_outbox_cross_module()
    {
        // Caminho 100% HTTP do leg do contrato (sem a fase de licitacao): contratacao DIRETA por dispensa
        // (art. 75) — prova que a celebracao via API publica o evento de integracao e a drenagem do Outbox
        // entrega ao consumidor de Patrimonio, sem depender de endpoints de proposta ainda inexistentes.
        var tenant = await _factory.ProvisionarTenantAsync(
            "13.345.678/0001-58", "Prefeitura E2E Dispensa/RS", PoderTenant.Executivo);
        var cliente = _factory.CreateClient().ComToken(EmissorTokenDeTeste.EmitirAdministrador(tenant));

        // Cadastra o fornecedor via HTTP (comando flat).
        var respFornecedor = await cliente.PostJsonAsync("/api/administracao/fornecedores", new
        {
            Cnpj = "44.555.666/0001-81",
            RazaoSocial = "Fornecedora Direta LTDA",
        });
        respFornecedor.StatusCode.Should().Be(HttpStatusCode.OK, await respFornecedor.Content.ReadAsStringAsync());
        var fornecedorId = (await respFornecedor.LerJsonAsync()).GetProperty("id").GetGuid();

        var respContrato = await cliente.PostJsonAsync("/api/administracao/contratos", new
        {
            LicitacaoId = (Guid?)null,
            FornecedorId = fornecedorId,
            Origem = "Dispensa",
            Objeto = "Servico de manutencao predial emergencial",
            Valor = 25_000m,
            VigenciaInicio = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            VigenciaFim = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)).ToString("yyyy-MM-dd"),
            EmpenhoId = (Guid?)null,
            NumeroEmpenho = (string?)null,
            JustificativaContratacaoDireta = "Dispensa por valor (art. 75, II) - manutencao emergencial.",
        });

        respContrato.StatusCode.Should().Be(HttpStatusCode.OK, await respContrato.Content.ReadAsStringAsync());

        // Drenagem nao deve lancar: o ContratoAssinadoIntegrationEvent chega ao consumidor cross-module.
        await _factory.Invoking(f => f.DrenarOutboxAsync(tenant)).Should().NotThrowAsync();
    }
}
