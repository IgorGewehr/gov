using System.Net;
using FluentAssertions;
using Tensorroot.Gov.IntegrationTests.Infrastructure;
using Tensorroot.Gov.Platform.Tenancy;
using Xunit;

namespace Tensorroot.Gov.IntegrationTests;

/// <summary>
/// W9.1 — E2E HTTP da INVARIANTE DE BLOQUEIO do empenho (Lei 14.133/2021, art. 94): o contrato so empenha
/// depois de divulgado no PNCP (numero de controle). Prova ponta-a-ponta, cross-module (Financas consulta
/// o status PNCP do Administracao via Contracts, em escopo dedicado):
/// <list type="number">
///   <item>celebra o contrato (Origem = Dispensa) via HTTP — nasce SEM PNCP;</item>
///   <item>tenta empenhar informando o ContratoId -> BARRADO (sem numero de controle, contrato ineficaz);</item>
///   <item>divulga no PNCP via HTTP -> a ACL (IPncpGateway simulado) devolve o numero de controle;</item>
///   <item>empenha de novo -> LIBERADO (200).</item>
/// </list>
/// O empenho SEM contrato (folha/despesa direta) permanece livre — o bloqueio so incide quando ha contrato.
/// </summary>
[Collection(ApiHostCollection.Nome)]
public sealed class PncpBloqueioEmpenhoE2ETests(ApiHostFixture fixture)
{
    private readonly CustomWebApplicationFactory _factory = fixture.Factory;

    [Fact]
    public async Task Contrato_sem_pncp_barra_empenho_e_apos_divulgacao_libera()
    {
        // Arrange: tenant proprio (banco dedicado) + token de administrador.
        var tenant = await _factory.ProvisionarTenantAsync(
            "18.345.678/0001-71", "Prefeitura E2E PNCP/RS", PoderTenant.Executivo);
        var cliente = _factory.CreateClient().ComToken(EmissorTokenDeTeste.EmitirAdministrador(tenant));

        // 1) Dotacao orcamentaria (credito da LOA) para sustentar o empenho.
        var respDotacao = await cliente.PostJsonAsync("/api/financas/dotacoes", new
        {
            Exercicio = 2026,
            Orgao = "01",
            Unidade = "01.01",
            FuncionalProgramatica = "04.122.0001.2001",
            CategoriaEconomica = "DespesasCorrentes",
            FonteRecurso = "1500",
            ValorDotado = 500_000m,
        });
        respDotacao.StatusCode.Should().Be(HttpStatusCode.OK, await respDotacao.Content.ReadAsStringAsync());
        var dotacaoId = (await respDotacao.LerJsonAsync()).GetProperty("id").GetGuid();

        // 2) Fornecedor.
        var respForn = await cliente.PostJsonAsync("/api/administracao/fornecedores", new
        {
            Cnpj = "45.555.666/0001-44",
            RazaoSocial = "Prestadora de Servicos LTDA",
        });
        respForn.StatusCode.Should().Be(HttpStatusCode.OK, await respForn.Content.ReadAsStringAsync());
        var fornecedorId = (await respForn.LerJsonAsync()).GetProperty("id").GetGuid();

        // 3) Celebra o contrato (Dispensa) — nasce ASSINADO, sem PNCP.
        var respContrato = await cliente.PostJsonAsync("/api/administracao/contratos", new
        {
            LicitacaoId = (Guid?)null,
            FornecedorId = fornecedorId,
            Origem = "Dispensa",
            Objeto = "Manutencao predial emergencial",
            Valor = 40_000m,
            VigenciaInicio = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            VigenciaFim = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)).ToString("yyyy-MM-dd"),
            EmpenhoId = (Guid?)null,
            NumeroEmpenho = (string?)null,
            JustificativaContratacaoDireta = "Dispensa por valor (art. 75, II).",
        });
        respContrato.StatusCode.Should().Be(HttpStatusCode.OK, await respContrato.Content.ReadAsStringAsync());
        var contratoId = (await respContrato.LerJsonAsync()).GetProperty("id").GetGuid();

        // 4) BLOQUEIO: tentar empenhar a despesa DESTE contrato sem PNCP -> recusado.
        var respEmpenhoBarrado = await cliente.PostJsonAsync("/api/financas/empenhos", new
        {
            Numero = "2026NE000010",
            DotacaoId = dotacaoId,
            TipoEmpenho = "Ordinario",
            Exercicio = 2026,
            DataEmpenho = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            CredorNome = "Prestadora de Servicos LTDA",
            CredorTipo = "Juridica",
            CredorDocumento = "45.555.666/0001-44",
            Valor = 40_000m,
            ContratoId = contratoId,
        });
        respEmpenhoBarrado.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "contrato sem numero de controle PNCP e ineficaz (art. 94) e NAO pode empenhar");
        ((int)respEmpenhoBarrado.StatusCode).Should().BeGreaterThanOrEqualTo(400);

        // 5) Divulga no PNCP via HTTP: a ACL (IPncpGateway simulado) transmite e devolve o numero de controle.
        var respPncp = await cliente.PostJsonAsync($"/api/administracao/contratos/{contratoId}/contrato-pncp", new
        {
            CnpjOrgao = "18345678000171",
            CodigoUnidade = "01.01",
            NumeroContratoInterno = "0010/2026",
            DocumentoFornecedor = "45555666000144",
        });
        respPncp.StatusCode.Should().Be(HttpStatusCode.NoContent, await respPncp.Content.ReadAsStringAsync());

        // Confirma que o contrato passou a publicado no PNCP (numero de controle gravado).
        var detalhe = await (await cliente.GetAsync($"/api/administracao/contratos/{contratoId}")).LerJsonAsync();
        detalhe.GetProperty("publicadoNoPncp").GetBoolean().Should().BeTrue();

        // 6) LIBERADO: agora o mesmo empenho passa.
        var respEmpenhoOk = await cliente.PostJsonAsync("/api/financas/empenhos", new
        {
            Numero = "2026NE000011",
            DotacaoId = dotacaoId,
            TipoEmpenho = "Ordinario",
            Exercicio = 2026,
            DataEmpenho = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            CredorNome = "Prestadora de Servicos LTDA",
            CredorTipo = "Juridica",
            CredorDocumento = "45.555.666/0001-44",
            Valor = 40_000m,
            ContratoId = contratoId,
        });
        respEmpenhoOk.StatusCode.Should().Be(HttpStatusCode.OK, await respEmpenhoOk.Content.ReadAsStringAsync());
        (await respEmpenhoOk.LerJsonAsync()).GetProperty("id").GetGuid().Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Empenho_sem_contrato_nao_e_bloqueado_pela_invariante_pncp()
    {
        var tenant = await _factory.ProvisionarTenantAsync(
            "19.345.678/0001-34", "Prefeitura E2E Empenho Livre/RS", PoderTenant.Executivo);
        var cliente = _factory.CreateClient().ComToken(EmissorTokenDeTeste.EmitirAdministrador(tenant));

        var respDotacao = await cliente.PostJsonAsync("/api/financas/dotacoes", new
        {
            Exercicio = 2026,
            Orgao = "01",
            Unidade = "01.01",
            FuncionalProgramatica = "04.122.0001.2001",
            CategoriaEconomica = "DespesasCorrentes",
            FonteRecurso = "1500",
            ValorDotado = 100_000m,
        });
        var dotacaoId = (await respDotacao.LerJsonAsync()).GetProperty("id").GetGuid();

        // Empenho SEM ContratoId (ex.: folha/despesa legal direta): a invariante PNCP nao incide.
        var resp = await cliente.PostJsonAsync("/api/financas/empenhos", new
        {
            Numero = "2026NE000020",
            DotacaoId = dotacaoId,
            TipoEmpenho = "Ordinario",
            Exercicio = 2026,
            DataEmpenho = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            CredorNome = "Folha de Pagamento",
            CredorTipo = "Juridica",
            CredorDocumento = "19.345.678/0001-34",
            Valor = 10_000m,
            ContratoId = (Guid?)null,
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK, await resp.Content.ReadAsStringAsync());
    }
}
