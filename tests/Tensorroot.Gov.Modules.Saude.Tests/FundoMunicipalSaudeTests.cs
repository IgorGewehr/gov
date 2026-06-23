using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Saude.Application.Fiscal;
using Tensorroot.Gov.Modules.Saude.Domain.Fiscal;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Fiscal;
using Xunit;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// Cobertura de integração do núcleo fiscal de Saúde S-2 (FMS por bloco) e S-1 (apuração via read
/// model), sobre SQLite em memória com isolamento por tenant: blocos do FMS segregados, transposição
/// vedada, apuração ASPS ponta a ponta e isolamento entre tenants (Global Query Filter).
/// </summary>
public sealed class FundoMunicipalSaudeTests : SaudeTestBase
{
    [Fact]
    public void FMS_segrega_a_execucao_por_bloco_de_financiamento()
    {
        var fundo = FundoMunicipalSaude.Criar(TenantA, "FMS Teste", "12345678000199");

        // Custeio (Bloco de Manutenção) e Investimento (Bloco de Estruturação) — Port. 3.992/2017.
        fundo.ReceberParcela(BlocoFinanciamentoSaude.Custeio, "fonte-custeio", 100_000m);
        fundo.ReceberParcela(BlocoFinanciamentoSaude.Investimento, "fonte-invest", 60_000m);
        fundo.ExecutarDespesa(BlocoFinanciamentoSaude.Custeio, "fonte-custeio", 40_000m);

        fundo.RecebidoDoBloco(BlocoFinanciamentoSaude.Custeio).Should().Be(100_000m);
        fundo.ExecutadoDoBloco(BlocoFinanciamentoSaude.Custeio).Should().Be(40_000m);
        fundo.SaldoDoBloco(BlocoFinanciamentoSaude.Custeio).Should().Be(60_000m);
        // O bloco Investimento é independente — execução de Custeio não o toca.
        fundo.SaldoDoBloco(BlocoFinanciamentoSaude.Investimento).Should().Be(60_000m);
        fundo.ExecutadoDoBloco(BlocoFinanciamentoSaude.Investimento).Should().Be(0m);
    }

    [Fact]
    public void FMS_veda_transposicao_entre_blocos()
    {
        var fundo = FundoMunicipalSaude.Criar(TenantA, "FMS Teste", "12345678000199");
        fundo.ReceberParcela(BlocoFinanciamentoSaude.Custeio, "fonte-custeio", 50_000m);

        // Tentar executar no Investimento sem saldo nele (mesmo havendo saldo no Custeio) → bloqueado.
        var executar = () => fundo.ExecutarDespesa(BlocoFinanciamentoSaude.Investimento, "fonte-invest", 10_000m);
        executar.Should().Throw<InvalidOperationException>().WithMessage("*vedada*");

        // Executar além do saldo do próprio bloco também é bloqueado.
        var excede = () => fundo.ExecutarDespesa(BlocoFinanciamentoSaude.Custeio, "fonte-custeio", 60_000m);
        excede.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task FMS_persiste_e_recarrega_contas_por_bloco()
    {
        var fundoId = FundoMunicipalSaudeId.New();
        await using (var ctx = CriarContexto(TenantA))
        {
            var fundo = FundoMunicipalSaude.Criar(TenantA, "FMS Maximiliano", "12345678000199");
            await ctx.FundosMunicipaisSaude.AddAsync(fundo);
            fundo.ReceberParcela(BlocoFinanciamentoSaude.Custeio, "fonte-custeio", 90_000m);
            fundo.ReceberParcela(BlocoFinanciamentoSaude.Investimento, "fonte-invest", 30_000m);
            await ctx.SaveChangesAsync();
            fundoId = fundo.Id;
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var recarregado = await ctx.FundosMunicipaisSaude.FirstAsync(f => f.Id == fundoId);
            recarregado.Contas.Should().HaveCount(2);
            recarregado.RecebidoDoBloco(BlocoFinanciamentoSaude.Custeio).Should().Be(90_000m);
            recarregado.RecebidoDoBloco(BlocoFinanciamentoSaude.Investimento).Should().Be(30_000m);
        }
    }

    [Fact]
    public async Task FMS_isola_por_tenant()
    {
        FundoMunicipalSaudeId idDoA;
        await using (var ctx = CriarContexto(TenantA))
        {
            var fundo = FundoMunicipalSaude.Criar(TenantA, "FMS A", "12345678000199");
            await ctx.FundosMunicipaisSaude.AddAsync(fundo);
            await ctx.SaveChangesAsync();
            idDoA = fundo.Id;
        }

        // O tenant B não enxerga o fundo do tenant A (Global Query Filter).
        await using (var ctx = CriarContexto(TenantB))
        {
            (await ctx.FundosMunicipaisSaude.AnyAsync(f => f.Id == idDoA)).Should().BeFalse();
            (await ctx.FundosMunicipaisSaude.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task Apuracao_ASPS_via_read_model_exclui_o_nao_computavel_e_isola_por_tenant()
    {
        const int exercicio = 2026;

        await using (var ctx = CriarContexto(TenantA))
        {
            foreach (var regra in SeedRegrasAsps.Gerar(TenantA))
            {
                await ctx.RegrasClassificacaoAsps.AddAsync(regra);
            }

            await ctx.LinhasExecucaoSaude.AddAsync(LinhaExecucaoSaude.ReceitaBase(TenantA, exercicio, 1_000_000m, "rec-1"));
            await ctx.LinhasExecucaoSaude.AddAsync(LinhaExecucaoSaude.Despesa(TenantA, exercicio, "10", "301", null, 150_000m, "dsp-1"));
            await ctx.LinhasExecucaoSaude.AddAsync(LinhaExecucaoSaude.Despesa(TenantA, exercicio, "10", "512", null, 50_000m, "dsp-2")); // saneamento — exclui
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var execucao = new ExecucaoSaudeReadModel(ctx);
            var regras = new RegraClassificacaoAspsRepository(ctx);
            var parametros = new ParametroAspsProvider(ctx);
            var handler = new ApurarAspsHandler(execucao, regras, parametros);

            var resultado = await handler.Handle(new ApurarAspsQuery(exercicio), CancellationToken.None);

            resultado.ReceitaBase.Should().Be(1_000_000m);
            resultado.AplicadoAsps.Should().Be(150_000m); // saneamento ficou de fora
            resultado.PercentualAplicado.Should().Be(0.15m);
            resultado.PercentualMinimo.Should().Be(0.15m); // default legal quando tenant não parametrizou
            resultado.Atingido.Should().BeTrue();
        }

        // Tenant B, sem dados, apura base zerada (isolamento).
        await using (var ctx = CriarContexto(TenantB))
        {
            var handler = new ApurarAspsHandler(
                new ExecucaoSaudeReadModel(ctx),
                new RegraClassificacaoAspsRepository(ctx),
                new ParametroAspsProvider(ctx));

            var resultado = await handler.Handle(new ApurarAspsQuery(exercicio), CancellationToken.None);
            resultado.ReceitaBase.Should().Be(0m);
            resultado.AplicadoAsps.Should().Be(0m);
            resultado.Atingido.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Percentual_minimo_versionado_sobrepoe_o_default_legal()
    {
        const int exercicio = 2026;
        await using var ctx = CriarContexto(TenantA);

        // Lei Orgânica do tenant exige 18% a partir de 2025.
        await ctx.ParametrosFiscaisSaude.AddAsync(
            ParametroFiscalSaude.Criar(TenantA, ParametroFiscalSaude.ChavePercentualMinimoAsps, new DateOnly(2025, 1, 1), 0.18m));
        await ctx.SaveChangesAsync();

        var provider = new ParametroAspsProvider(ctx);
        (await provider.ObterPercentualMinimoAsync(exercicio, CancellationToken.None)).Should().Be(0.18m);
        // Antes da vigência (2024) cai no default legal.
        (await provider.ObterPercentualMinimoAsync(2024, CancellationToken.None)).Should().Be(0.15m);
    }
}
