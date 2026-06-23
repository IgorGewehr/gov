using FluentAssertions;
using Tensorroot.Gov.Modules.Financas.Contracts;
using PagamentoEfetuadoIntegrationEvent = Tensorroot.Gov.Modules.Financas.Contracts.PagamentoEfetuadoIntegrationEvent;
using Tensorroot.Gov.Modules.PainelGestor.Application.Indicadores;
using Tensorroot.Gov.Modules.PainelGestor.Application.Ingestao;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Limites;
using Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Limites;
using Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.Modules.RecursosHumanos.Contracts;
using Tensorroot.Gov.Modules.Transparencia.Contracts;
using Tensorroot.Gov.Modules.Tributos.Contracts;
using Xunit;

namespace Tensorroot.Gov.Modules.PainelGestor.Tests;

/// <summary>
/// Testes de integração da ingestão de Integration Events (ACL de entrada) e da agregação dos KPIs do
/// Painel do Gestor: prova a materialização cross-module via Contracts, a idempotência (I-13), a
/// reprodutibilidade (sem relógio) e o isolamento por tenant (Global Query Filter).
/// </summary>
public sealed class IngestaoIndicadoresTests : PainelGestorTestBase
{
    private const int Exercicio = 2026;

    // Cada handler é construído sobre um contexto NOVO (escopo de mensagem do Outbox isola por mensagem) —
    // espelha o ScopedOutboxMessageDispatcher do ApiHost. A consulta usa outro contexto, simulando o GET.
    private static MaterializadorIndicadores Materializador(PainelGestorDbContext ctx) => new(new IndicadorMunicipioRepository(ctx));

    private static IngestaoIdempotencia Idem(PainelGestorDbContext ctx) => new(ctx, TimeProvider.System);

    [Fact]
    public async Task Agrega_execucao_orcamentaria_a_partir_de_eventos_de_financas()
    {
        // Dotação (denominador) + empenhado + liquidado + pago (acumuladores), via eventos de Finanças.
        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberDotacaoOrcamentariaHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new DotacaoOrcamentariaPublicadaIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantA, Exercicio, 800_000m, 1_000_000m),
                default);
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberDespesaEmpenhadaHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new DespesaEmpenhadaIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantA, Guid.NewGuid(), "NE-1", "339030", 300_000m, "Fornecedor", "00000000000191", Competencia: new DateOnly(Exercicio, 3, 1)),
                default);
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberDespesaLiquidadaHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new DespesaLiquidadaIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantA, Guid.NewGuid(), Guid.NewGuid(), 200_000m, new DateOnly(Exercicio, 4, 1)),
                default);
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberPagamentoEfetuadoHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new PagamentoEfetuadoIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantA, Guid.NewGuid(), "OP-1", 150_000m, new DateOnly(Exercicio, 5, 1)),
                default);
        }

        var painel = await ConsultarAsync(TenantA);

        painel.ExecucaoOrcamentaria.DotacaoAtualizada.Should().Be(1_000_000m);
        painel.ExecucaoOrcamentaria.Empenhado.Should().Be(300_000m);
        painel.ExecucaoOrcamentaria.Liquidado.Should().Be(200_000m);
        painel.ExecucaoOrcamentaria.Pago.Should().Be(150_000m);
        painel.ExecucaoOrcamentaria.PercentualEmpenhado.Should().Be(0.30m);
        painel.ExecucaoOrcamentaria.PercentualLiquidado.Should().Be(0.20m);
        painel.ExecucaoOrcamentaria.PercentualPago.Should().Be(0.15m);
    }

    [Fact]
    public async Task Calcula_percentual_da_rcl_lrf_a_partir_de_pessoal_e_rcl()
    {
        // Despesa de pessoal (RH) + RCL (Finanças) → % da RCL e semáforo LRF.
        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberDespesaPessoalHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new DespesaPessoalApuradaIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantA, Exercicio, 6, "Mensal", 520_000m),
                default);
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberRclApuradaHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new ReceitaCorrenteLiquidaApuradaIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantA, Exercicio, 6, 1_000_000m),
                default);
        }

        var painel = await ConsultarAsync(TenantA);

        // 520.000 / 1.000.000 = 52% → entre prudencial (51,3%) e legal (54%) → Alerta (amarelo).
        painel.PessoalLrf.DespesaPessoal.Should().Be(520_000m);
        painel.PessoalLrf.ReceitaCorrenteLiquida.Should().Be(1_000_000m);
        painel.PessoalLrf.PercentualDaRcl.Should().Be(0.52m);
        painel.PessoalLrf.LimiteLegal.Should().Be(LimitesPessoalLrf.LimiteLegalExecutivoMunicipalPadrao);
        painel.PessoalLrf.Situacao.Should().Be(SituacaoLimite.Alerta);
    }

    [Fact]
    public async Task Despesa_pessoal_acumula_competencias_e_rcl_mantem_mes_mais_recente()
    {
        // Duas competências de pessoal somam; duas RCL → vale a do mês mais recente (janela 12m mais atual).
        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberDespesaPessoalHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new DespesaPessoalApuradaIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantA, Exercicio, 1, "Mensal", 100_000m), default);
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberDespesaPessoalHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new DespesaPessoalApuradaIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantA, Exercicio, 2, "Mensal", 120_000m), default);
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberRclApuradaHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new ReceitaCorrenteLiquidaApuradaIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantA, Exercicio, 3, 900_000m), default);
        }

        // RCL de mês anterior (2) chega depois — NÃO deve sobrescrever a do mês 3.
        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberRclApuradaHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new ReceitaCorrenteLiquidaApuradaIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantA, Exercicio, 2, 700_000m), default);
        }

        var painel = await ConsultarAsync(TenantA);
        painel.PessoalLrf.DespesaPessoal.Should().Be(220_000m);
        painel.PessoalLrf.ReceitaCorrenteLiquida.Should().Be(900_000m);
    }

    [Fact]
    public async Task Reentrega_do_mesmo_evento_e_idempotente_nao_conta_duas_vezes()
    {
        var evento = new DespesaEmpenhadaIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, TenantA, Guid.NewGuid(), "NE-1", "339030", 100_000m, "Fornecedor", "00000000000191",
            Competencia: new DateOnly(Exercicio, 3, 1));

        // Mesmo EventId entregue DUAS vezes (Outbox at-least-once).
        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberDespesaEmpenhadaHandler(Materializador(ctx), Idem(ctx), ctx).Handle(evento, default);
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberDespesaEmpenhadaHandler(Materializador(ctx), Idem(ctx), ctx).Handle(evento, default);
        }

        var painel = await ConsultarAsync(TenantA);
        painel.ExecucaoOrcamentaria.Empenhado.Should().Be(100_000m); // contou UMA vez.
    }

    [Fact]
    public async Task Resultado_e_reproduzivel_para_a_mesma_entrada()
    {
        await SemearArrecadacaoEMinimosAsync(TenantA);

        var primeira = await ConsultarAsync(TenantA);
        var segunda = await ConsultarAsync(TenantA);

        segunda.Should().BeEquivalentTo(primeira); // mesma entrada → mesmo painel (sem relógio no cálculo).
    }

    [Fact]
    public async Task Isolamento_por_tenant_nao_vaza_dados_entre_entes()
    {
        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberReceitaArrecadadaHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new ReceitaArrecadadaIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantA, Guid.NewGuid(), 500_000m, new DateOnly(Exercicio, 2, 1)), default);
        }

        await using (var ctx = CriarContexto(TenantB))
        {
            await new ReceberReceitaArrecadadaHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new ReceitaArrecadadaIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantB, Guid.NewGuid(), 999_000m, new DateOnly(Exercicio, 2, 1)), default);
        }

        var painelA = await ConsultarAsync(TenantA);
        var painelB = await ConsultarAsync(TenantB);

        painelA.Arrecadacao.ArrecadacaoTributaria.Should().Be(500_000m);
        painelB.Arrecadacao.ArrecadacaoTributaria.Should().Be(999_000m);
    }

    [Fact]
    public async Task Agrega_minimos_arrecadacao_divida_e_prontidao()
    {
        await SemearArrecadacaoEMinimosAsync(TenantA);

        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberPosicaoDividaAtivaHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new PosicaoDividaAtivaIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantA, Exercicio, 2_000_000m, 800_000m, 120_000m), default);
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberRemessaEnviadaHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new RemessaEnviadaTceIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantA, Guid.NewGuid(), $"1º Bimestre/{Exercicio}", new DateOnly(Exercicio, 3, 30)), default);
        }

        var painel = await ConsultarAsync(TenantA);

        painel.Arrecadacao.ArrecadacaoTributaria.Should().Be(500_000m);
        painel.Arrecadacao.DividaAtivaSaldoInscrito.Should().Be(2_000_000m);
        painel.Arrecadacao.DividaAtivaRecuperada.Should().Be(120_000m);

        painel.Minimos.Should().HaveCount(2);
        painel.Minimos.Should().Contain(m => m.Setor == "Saude" && m.Situacao == "Atingido");

        painel.PrestacaoContas.RemessasEnviadas.Should().Be(1);
        painel.PrestacaoContas.RemessasComPrazoVencido.Should().Be(0);
        painel.PrestacaoContas.EmDia.Should().BeTrue();
        painel.PrestacaoContas.Situacao.Should().Be(SituacaoLimite.Adequado);
    }

    [Fact]
    public async Task Prazo_vencido_marca_prestacao_de_contas_como_excedido()
    {
        await using (var ctx = CriarContexto(TenantA))
        {
            await new ReceberPrazoRemessaVencidoHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new PrazoRemessaVencidoIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, TenantA, Guid.NewGuid(), $"1º Bimestre/{Exercicio}", new DateOnly(Exercicio, 3, 30)), default);
        }

        var painel = await ConsultarAsync(TenantA);
        painel.PrestacaoContas.RemessasComPrazoVencido.Should().Be(1);
        painel.PrestacaoContas.EmDia.Should().BeFalse();
        painel.PrestacaoContas.Situacao.Should().Be(SituacaoLimite.Excedido);
    }

    [Fact]
    public async Task Exercicio_sem_dados_degrada_graciosamente_sem_lancar()
    {
        var painel = await ConsultarAsync(TenantA);

        painel.Exercicio.Should().Be(Exercicio);
        painel.ExecucaoOrcamentaria.PercentualEmpenhado.Should().Be(0m);
        painel.PessoalLrf.Situacao.Should().Be(SituacaoLimite.Indeterminado);
        painel.PrestacaoContas.Situacao.Should().Be(SituacaoLimite.Indeterminado);
        painel.Minimos.Should().BeEmpty();
    }

    private async Task SemearArrecadacaoEMinimosAsync(Guid tenant)
    {
        await using (var ctx = CriarContexto(tenant))
        {
            await new ReceberReceitaArrecadadaHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new ReceitaArrecadadaIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, tenant, Guid.NewGuid(), 500_000m, new DateOnly(Exercicio, 2, 1)), default);
        }

        await using (var ctx = CriarContexto(tenant))
        {
            var setores = new List<MinimoSetorialApuradoDto>
            {
                new("Saude", 4_000_000m, 700_000m, 0.175m, 0.15m, "Atingido"),
                new("Educacao", 4_000_000m, 1_100_000m, 0.275m, 0.25m, "Atingido"),
            };
            await new ReceberMinimoConstitucionalHandler(Materializador(ctx), Idem(ctx), ctx).Handle(
                new MinimoConstitucionalApuradoIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, tenant, Exercicio, setores), default);
        }
    }

    private async Task<PainelGestorDto> ConsultarAsync(Guid tenant)
    {
        await using var ctx = CriarContexto(tenant);
        var handler = new ObterPainelGestorHandler(new IndicadorMunicipioRepository(ctx), new LimitesPessoalProvider(ctx));
        return await handler.Handle(new ObterPainelGestorQuery(Exercicio), default);
    }
}
