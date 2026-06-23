using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Educacao.Application.Fiscal;
using Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;
using Tensorroot.Gov.Modules.Educacao.Infrastructure.Fiscal;
using Xunit;

namespace Tensorroot.Gov.Modules.Educacao.Tests;

/// <summary>
/// Cobertura de integração do núcleo fiscal de Educação (E-1 MDE via read model, E-2 piso de 70% do
/// FUNDEB cruzando com a folha, E-3 distribuição por origem), sobre SQLite em memória com isolamento por
/// tenant: a MDE computa só o classificado (exclui o art. 71), o 70% atinge/não conforme a remuneração, a
/// distribuição persiste/recarrega e tudo isola por tenant (Global Query Filter).
/// </summary>
public sealed class FiscalEducacaoIntegracaoTests : EducacaoTestBase
{
    private const int Exercicio = 2026;

    [Fact]
    public async Task Apuracao_MDE_via_read_model_exclui_o_nao_computavel_e_isola_por_tenant()
    {
        await using (var ctx = CriarContexto(TenantA))
        {
            foreach (var regra in SeedRegrasMde.Gerar(TenantA))
            {
                await ctx.RegrasClassificacaoMde.AddAsync(regra);
            }

            await ctx.LinhasExecucaoEducacao.AddAsync(LinhaExecucaoEducacao.ReceitaBase(TenantA, Exercicio, 1_000_000m, "rec-1"));
            await ctx.LinhasExecucaoEducacao.AddAsync(LinhaExecucaoEducacao.Despesa(TenantA, Exercicio, "12", "361", null, 250_000m, "dsp-1"));
            await ctx.LinhasExecucaoEducacao.AddAsync(LinhaExecucaoEducacao.Despesa(TenantA, Exercicio, "12", "306", null, 50_000m, "dsp-2")); // merenda — exclui
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new ApurarMdeHandler(
                new ExecucaoEducacaoReadModel(ctx),
                new RegraClassificacaoMdeRepository(ctx),
                new ParametroMdeProvider(ctx));

            var resultado = await handler.Handle(new ApurarMdeQuery(Exercicio), CancellationToken.None);

            resultado.ReceitaBase.Should().Be(1_000_000m);
            resultado.AplicadoMde.Should().Be(250_000m); // merenda ficou de fora
            resultado.PercentualAplicado.Should().Be(0.25m);
            resultado.PercentualMinimo.Should().Be(0.25m); // default legal quando tenant não parametrizou
            resultado.Atingido.Should().BeTrue();
            resultado.EhConformidade.Should().BeTrue(); // aferição anual por default
        }

        // Tenant B, sem dados, apura base zerada (isolamento).
        await using (var ctx = CriarContexto(TenantB))
        {
            var handler = new ApurarMdeHandler(
                new ExecucaoEducacaoReadModel(ctx),
                new RegraClassificacaoMdeRepository(ctx),
                new ParametroMdeProvider(ctx));

            var resultado = await handler.Handle(new ApurarMdeQuery(Exercicio), CancellationToken.None);
            resultado.ReceitaBase.Should().Be(0m);
            resultado.AplicadoMde.Should().Be(0m);
            resultado.Atingido.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Feeder_MDE_projeta_linhas_e_e_idempotente_por_origem()
    {
        await using var ctx = CriarContexto(TenantA);
        var repo = new LinhaExecucaoEducacaoRepository(ctx, new TenantContextFake(TenantA));
        var handler = new RegistrarExecucaoEducacaoHandler(repo, ctx);

        var comando = new RegistrarExecucaoEducacaoCommand(Exercicio,
        [
            new LinhaExecucaoEducacaoEntrada(TipoLinhaExecucaoEducacao.ReceitaBaseImpostosTransferencias, null, null, null, 1_000_000m, "rec-1"),
            new LinhaExecucaoEducacaoEntrada(TipoLinhaExecucaoEducacao.DespesaEducacao, "12", "361", null, 250_000m, "dsp-1"),
        ]);

        (await handler.Handle(comando, CancellationToken.None)).Should().Be(2);
        // Reenvio da MESMA origem não duplica (idempotência).
        (await handler.Handle(comando, CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task FUNDEB_70_atingido_quando_remuneracao_cruzada_com_a_folha_alcanca_o_piso()
    {
        DistribuicaoFundebId distribuicaoId;
        await using (var ctx = CriarContexto(TenantA))
        {
            var distribuicao = DistribuicaoFundeb.Criar(TenantA, Exercicio);
            distribuicao.ReceberParcela(OrigemRecursoFundeb.CotaParteEstadual, 900_000m);
            distribuicao.ReceberParcela(OrigemRecursoFundeb.ComplementacaoVaat, 100_000m);
            await ctx.DistribuicoesFundeb.AddAsync(distribuicao);
            await ctx.SaveChangesAsync();
            distribuicaoId = distribuicao.Id;
        }

        // Cruzamento com a folha (RH/Contracts ou parâmetro): 700k pagos a profissionais = 70% de 1M.
        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new RegistrarRemuneracaoMagisterioHandler(
                new RemuneracaoMagisterioReadModel(ctx, new TenantContextFake(TenantA)), ctx);
            await handler.Handle(new RegistrarRemuneracaoMagisterioCommand(Exercicio, 700_000m), CancellationToken.None);
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new ApurarFundeb70Handler(
                new DistribuicaoFundebRepository(ctx),
                new RemuneracaoMagisterioReadModel(ctx, new TenantContextFake(TenantA)),
                new ParametroFundebProvider(ctx));

            var resultado = await handler.Handle(new ApurarFundeb70Query(Exercicio), CancellationToken.None);

            resultado.ReceitaFundeb.Should().Be(1_000_000m);
            resultado.RemuneracaoProfissionais.Should().Be(700_000m);
            resultado.PercentualAplicado.Should().Be(0.70m);
            resultado.PisoMinimo.Should().Be(0.70m); // default legal EC 108/2020
            resultado.Atingido.Should().BeTrue();
        }

        _ = distribuicaoId;
    }

    [Fact]
    public async Task FUNDEB_70_nao_atingido_quando_remuneracao_fica_abaixo_do_piso()
    {
        await using (var ctx = CriarContexto(TenantA))
        {
            var distribuicao = DistribuicaoFundeb.Criar(TenantA, Exercicio);
            distribuicao.ReceberParcela(OrigemRecursoFundeb.CotaParteEstadual, 1_000_000m);
            await ctx.DistribuicoesFundeb.AddAsync(distribuicao);

            var remuneracao = new RemuneracaoMagisterioReadModel(ctx, new TenantContextFake(TenantA));
            await remuneracao.DefinirRemuneracaoProfissionaisAsync(Exercicio, 600_000m, CancellationToken.None); // 60% < 70%
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new ApurarFundeb70Handler(
                new DistribuicaoFundebRepository(ctx),
                new RemuneracaoMagisterioReadModel(ctx, new TenantContextFake(TenantA)),
                new ParametroFundebProvider(ctx));

            var resultado = await handler.Handle(new ApurarFundeb70Query(Exercicio), CancellationToken.None);

            resultado.PercentualAplicado.Should().Be(0.60m);
            resultado.Atingido.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Distribuicao_FUNDEB_persiste_recarrega_e_isola_por_tenant()
    {
        await using (var ctx = CriarContexto(TenantA))
        {
            var distribuicao = DistribuicaoFundeb.Criar(TenantA, Exercicio);
            distribuicao.DefinirEsperado(OrigemRecursoFundeb.CotaParteEstadual, 800_000m);
            distribuicao.ReceberParcela(OrigemRecursoFundeb.CotaParteEstadual, 500_000m);
            distribuicao.ReceberParcela(OrigemRecursoFundeb.ComplementacaoVaaf, 100_000m);
            await ctx.DistribuicoesFundeb.AddAsync(distribuicao);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var recarregada = await ctx.DistribuicoesFundeb.FirstAsync(d => d.Exercicio == Exercicio);
            recarregada.Contas.Should().HaveCount(2);
            recarregada.RecebidoDaOrigem(OrigemRecursoFundeb.CotaParteEstadual).Should().Be(500_000m);
            recarregada.DivergenciaDaOrigem(OrigemRecursoFundeb.CotaParteEstadual).Should().Be(-300_000m);
            recarregada.ReceitaFundebTotal.Should().Be(600_000m);
        }

        // O tenant B não enxerga a distribuição do tenant A (Global Query Filter).
        await using (var ctx = CriarContexto(TenantB))
        {
            (await ctx.DistribuicoesFundeb.AnyAsync(d => d.Exercicio == Exercicio)).Should().BeFalse();
            (await ctx.DistribuicoesFundeb.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task Percentual_minimo_MDE_versionado_sobrepoe_o_default_legal()
    {
        await using var ctx = CriarContexto(TenantA);

        // Lei Orgânica do tenant exige 28% a partir de 2025.
        await ctx.ParametrosFiscaisEducacao.AddAsync(
            ParametroFiscalEducacao.Criar(TenantA, ParametroFiscalEducacao.ChavePercentualMinimoMde, new DateOnly(2025, 1, 1), 0.28m));
        await ctx.SaveChangesAsync();

        var provider = new ParametroMdeProvider(ctx);
        (await provider.ObterPercentualMinimoAsync(Exercicio, CancellationToken.None)).Should().Be(0.28m);
        // Antes da vigência (2024) cai no default legal de 25%.
        (await provider.ObterPercentualMinimoAsync(2024, CancellationToken.None)).Should().Be(0.25m);
    }
}
