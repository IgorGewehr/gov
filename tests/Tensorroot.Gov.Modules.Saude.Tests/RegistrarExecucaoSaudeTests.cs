using FluentAssertions;
using Tensorroot.Gov.Modules.Saude.Application.Fiscal;
using Tensorroot.Gov.Modules.Saude.Domain.Fiscal;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Fiscal;
using Xunit;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// Cobertura do alimentador Via A2 da execução de Saúde (S-1): o <see cref="RegistrarExecucaoSaudeHandler"/>
/// projeta as linhas de receita-base/despesa de forma idempotente (por OrigemHash) e a apuração ASPS
/// classifica FINO sobre o que foi alimentado — uma despesa não-computável (inativos, subfunção 122) muda
/// o percentual aplicado. Sobre SQLite em memória, tenant-scoped.
/// </summary>
public sealed class RegistrarExecucaoSaudeTests : SaudeTestBase
{
    [Fact]
    public async Task Feeder_projeta_linhas_e_e_idempotente_por_origem()
    {
        const int exercicio = 2026;
        await using var ctx = CriarContexto(TenantA);
        var repo = new LinhaExecucaoSaudeRepository(ctx, new TenantContextFake(TenantA));
        var handler = new RegistrarExecucaoSaudeHandler(repo, ctx);

        var comando = new RegistrarExecucaoSaudeCommand(exercicio,
        [
            new LinhaExecucaoSaudeEntrada(TipoLinhaExecucaoSaude.ReceitaBaseImpostosTransferencias, null, null, null, 1_000_000m, "rec-1"),
            new LinhaExecucaoSaudeEntrada(TipoLinhaExecucaoSaude.DespesaSaude, "10", "301", null, 150_000m, "dsp-1"),
        ]);

        (await handler.Handle(comando, CancellationToken.None)).Should().Be(2);
        // Reenvio da MESMA origem não duplica (idempotência).
        (await handler.Handle(comando, CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task Despesa_nao_computavel_alimentada_via_feeder_muda_o_percentual_ASPS()
    {
        const int exercicio = 2026;

        await using (var ctx = CriarContexto(TenantA))
        {
            foreach (var regra in SeedRegrasAsps.Gerar(TenantA))
            {
                await ctx.RegrasClassificacaoAsps.AddAsync(regra);
            }

            await ctx.SaveChangesAsync();
        }

        // Alimenta via feeder: receita 1.000.000, ASPS computável (10.301) 150.000 e inativos (10.122) 80.000.
        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new RegistrarExecucaoSaudeHandler(new LinhaExecucaoSaudeRepository(ctx, new TenantContextFake(TenantA)), ctx);
            await handler.Handle(new RegistrarExecucaoSaudeCommand(exercicio,
            [
                new LinhaExecucaoSaudeEntrada(TipoLinhaExecucaoSaude.ReceitaBaseImpostosTransferencias, null, null, null, 1_000_000m, "rec-1"),
                new LinhaExecucaoSaudeEntrada(TipoLinhaExecucaoSaude.DespesaSaude, "10", "301", null, 150_000m, "dsp-301"),
                new LinhaExecucaoSaudeEntrada(TipoLinhaExecucaoSaude.DespesaSaude, "10", "122", null, 80_000m, "dsp-122-inativos"),
            ]), CancellationToken.None);
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new ApurarAspsHandler(
                new ExecucaoSaudeReadModel(ctx),
                new RegraClassificacaoAspsRepository(ctx),
                new ParametroAspsProvider(ctx));

            var resultado = await handler.Handle(new ApurarAspsQuery(exercicio), CancellationToken.None);

            // Os inativos (subfunção 122) NÃO computam: aplicado fica em 150.000 (15%), não 230.000 (23%).
            resultado.AplicadoAsps.Should().Be(150_000m);
            resultado.PercentualAplicado.Should().Be(0.15m);
            resultado.Atingido.Should().BeTrue();
        }
    }
}
