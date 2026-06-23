using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.CicloAnual;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.TabelasLegais;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.CicloAnual;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Providers;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura de integracao dos handlers do ciclo anual (13o/ferias/rescisao) sobre SQLite: cada um gera
/// sua folha (Tipo proprio), reusa o catalogo de rubricas e as tabelas legais, e — no caso do 13o 2a
/// parcela — apura INSS/IRRF PROPRIOS em base separada pelo motor. Prova a coexistencia de folhas mensal
/// e do ciclo anual na mesma competencia (indice (TenantId, Competencia, Tipo)).
/// </summary>
public sealed class CicloAnualHandlersTests : RecursosHumanosTestBase
{
    private sealed class ParametrosFolhaProviderFake : IParametrosFolhaProvider
    {
        public Task<ParametrosFolha> ObterAsync(CancellationToken cancellationToken)
            => Task.FromResult(new ParametrosFolha { TetoRemuneratorio = 50000m });
    }

    private static async Task<Guid> SemearServidorAsync(RecursosHumanosDbContext ctx, RegimePrevidenciario regime)
    {
        var servidor = Servidor.Admitir(
            TenantA,
            Cpf.Create("52998224725"),
            Matricula.De("M-13"),
            DadosPessoais.Criar("Fulano de Tal", new DateOnly(1990, 1, 1)),
            CargoId.New(),
            regime,
            new DateOnly(2026, 1, 5));
        ctx.Servidores.Add(servidor);
        await ctx.SaveChangesAsync();
        return servidor.Id.Value;
    }

    private static void SemearTabelas(RecursosHumanosDbContext ctx)
    {
        foreach (var t in TabelasFederaisSeed.Inss(TenantA))
        {
            ctx.TabelasInss.Add(t);
        }

        foreach (var t in TabelasFederaisSeed.Irrf(TenantA))
        {
            ctx.TabelasIrrf.Add(t);
        }
    }

    private static GerarDecimoTerceiroHandler Handler13(RecursosHumanosDbContext ctx)
        => new(
            new FolhaDePagamentoRepository(ctx), new RubricaFolhaRepository(ctx),
            new ServidorRegimeConsulta(ctx), new TabelasLegaisProvider(ctx),
            new ParametrosFolhaProviderFake(), ctx, new TenantContextFake(TenantA));

    [Fact] // 13o 2a parcela: INSS-13 501,51 e IRRF-13 336,67 (base separada, sem simplificado) + abate da 1a parcela.
    public async Task Decimo_terceiro_segunda_parcela_apura_inss_irrf_proprios_e_abate_primeira()
    {
        Guid servidorId;
        var competencia = Competencia.De(2026, 12);

        await using (var ctx = CriarContexto(TenantA))
        {
            servidorId = await SemearServidorAsync(ctx, RegimePrevidenciario.Rgps);
            SemearTabelas(ctx);
            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("13-SAL"), "13o Salario", NaturezaRubrica.Provento, competencia, incideInss: true, incideIrrf: true));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            // Servidor 2026-01-05 -> 12 avos no ano; remuneracao-base 5000 -> integral 5000.
            await Handler13(ctx).Handle(
                new GerarDecimoTerceiroCommand(servidorId, 2026, 12, Parcela: 2, RemuneracaoBase: 5000m, Admissao: new DateOnly(2026, 1, 5)),
                default);

            var folha = await ctx.FolhasDePagamento.Include(f => f.Eventos)
                .SingleAsync(f => f.Tipo == TipoFolha.DecimoTerceiro);

            folha.BaseSeparada.Should().BeTrue();
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "13-SAL" && e.Tipo == TipoEvento.Provento && e.Valor == 5000m);
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "INSS-13" && e.Valor == 501.51m);
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "IRRF-13" && e.Valor == 336.67m);
            // Abate da 1a parcela ja paga (50% = 2500).
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "13-ADIANT" && e.Valor == 2500m);
        }
    }

    [Fact] // 13o 2a parcela NAO aplica abate-teto mesmo com teto baixo (folha propria, design §5).
    public async Task Decimo_terceiro_nao_aplica_abate_teto()
    {
        Guid servidorId;
        var competencia = Competencia.De(2026, 12);

        await using (var ctx = CriarContexto(TenantA))
        {
            servidorId = await SemearServidorAsync(ctx, RegimePrevidenciario.Rgps);
            SemearTabelas(ctx);
            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("13-SAL"), "13o", NaturezaRubrica.Provento, competencia, incideInss: true, incideIrrf: true));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            await Handler13(ctx).Handle(
                new GerarDecimoTerceiroCommand(servidorId, 2026, 12, 2, 5000m, new DateOnly(2026, 1, 5)), default);

            var folha = await ctx.FolhasDePagamento.Include(f => f.Eventos).SingleAsync(f => f.Tipo == TipoFolha.DecimoTerceiro);
            // Calcular com teto baixissimo: nao deve lancar abate-teto (folha de 13o nao aplica teto).
            folha.Calcular(tetoRemuneratorio: 100m, hoje: new DateOnly(2026, 12, 20));
            folha.Eventos.Should().NotContain(e => e.Rubrica.Codigo == FolhaDePagamento.CodigoRubricaAbateTetoPadrao);
        }
    }

    [Fact] // Ferias: gera FERIAS + 1/3 numa folha Tipo=Ferias.
    public async Task Ferias_gera_remuneracao_e_terco_em_folha_propria()
    {
        Guid servidorId;

        await using (var ctx = CriarContexto(TenantA))
        {
            servidorId = await SemearServidorAsync(ctx, RegimePrevidenciario.Rgps);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new GerarFeriasHandler(
                new FolhaDePagamentoRepository(ctx), new ServidorRegimeConsulta(ctx),
                new ParametrosFolhaProviderFake(), ctx, new TenantContextFake(TenantA));

            await handler.Handle(new GerarFeriasCommand(servidorId, 2026, 1, RemuneracaoMensal: 3000m, DiasGozados: 30, DiasVendidos: 0), default);

            var folha = await ctx.FolhasDePagamento.Include(f => f.Eventos).SingleAsync(f => f.Tipo == TipoFolha.Ferias);
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "FERIAS" && e.Valor == 3000m);
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "1/3-FERIAS" && e.Valor == 1000m);
        }
    }

    private static GerarVerbasRescisoriasHandler HandlerRescisao(RecursosHumanosDbContext ctx)
        => new(
            new FolhaDePagamentoRepository(ctx), new ServidorRegimeConsulta(ctx),
            new RubricaFolhaRepository(ctx), new TabelasLegaisProvider(ctx),
            new ParametrosFolhaProviderFake(), ctx, new TenantContextFake(TenantA));

    [Fact] // P0-6: ferias concedidas apos o concessivo sao pagas EM DOBRO (CLT art. 137) ligando o EmDobra.
    public async Task Ferias_em_dobra_quando_concedidas_apos_periodo_concessivo()
    {
        Guid servidorId;

        await using (var ctx = CriarContexto(TenantA))
        {
            servidorId = await SemearServidorAsync(ctx, RegimePrevidenciario.Rgps);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new GerarFeriasHandler(
                new FolhaDePagamentoRepository(ctx), new ServidorRegimeConsulta(ctx),
                new ParametrosFolhaProviderFake(), ctx, new TenantContextFake(TenantA));

            // Aquisitivo 2024-01-01; concessivo expira 2025-12-31; concessao em 2026-03 -> DOBRA.
            await handler.Handle(new GerarFeriasCommand(
                servidorId, 2026, 3, RemuneracaoMensal: 3000m, DiasGozados: 30, DiasVendidos: 0,
                InicioPeriodoAquisitivo: new DateOnly(2024, 1, 1), DataConcessao: new DateOnly(2026, 3, 1)), default);

            var folha = await ctx.FolhasDePagamento.Include(f => f.Eventos).SingleAsync(f => f.Tipo == TipoFolha.Ferias);
            // Em dobra: remuneracao 2x (6000) e 1/3 sobre a dobra (2000).
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "FERIAS" && e.Valor == 6000m);
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "1/3-FERIAS" && e.Valor == 2000m);
        }
    }

    [Fact] // Rescisao estatutaria (exoneracao): saldo + 13o prop + ferias vencidas + 1/3; SEM aviso/multa.
    public async Task Rescisao_estatutaria_compoe_verbas_sem_aviso_nem_multa()
    {
        Guid servidorId;
        var competencia = Competencia.De(2026, 6);

        await using (var ctx = CriarContexto(TenantA))
        {
            servidorId = await SemearServidorAsync(ctx, RegimePrevidenciario.Rpps);
            SemearTabelas(ctx);
            // Servidor efetivo (RPPS): tabela municipal exigida para apurar o 13o-prop em base separada.
            ctx.TabelasRpps.Add(TabelaRpps.Criar(
                TenantA, Competencia.De(2026, 1),
                new[] { FaixaProgressiva.De(0m, 999999m, 0.11m) },
                teto: null, baseLegal: "Lei Municipal de Previdencia (teste)"));
            // 13-PROP com incidencia RPPS/IRRF (P0-4): seus descontos sao apurados em base separada propria.
            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("13-PROP"), "13o Proporcional", NaturezaRubrica.Provento, competencia, incideRpps: true, incideIrrf: true));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = HandlerRescisao(ctx);

            // Desligamento 2026-06-15: saldo = 3000*15/30 = 1500.
            await handler.Handle(new GerarVerbasRescisoriasCommand(
                servidorId,
                DataDesligamento: new DateOnly(2026, 6, 15),
                TipoDesligamento: TipoDesligamento.ExoneracaoVacancia,
                Regime: RegimeVinculo.Estatutario,
                Vencimento: 3000m,
                DiasTrabalhadosNoMes: 15,
                Admissao: new DateOnly(2026, 1, 5),
                InicioPeriodoAquisitivoFerias: new DateOnly(2026, 1, 5),
                DiasFeriasVencidas: 30,
                ValorAvisoPrevio: 0m,
                ValorMultaFgts: 0m), default);

            var folha = await ctx.FolhasDePagamento.Include(f => f.Eventos).SingleAsync(f => f.Tipo == TipoFolha.Rescisao);
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "SALDO-SAL" && e.Valor == 1500m);
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "13-PROP");
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "FERIAS-VENC");
            folha.Eventos.Should().NotContain(e => e.Rubrica.Codigo == "AVISO-PREV");
            folha.Eventos.Should().NotContain(e => e.Rubrica.Codigo == "MULTA-FGTS");
        }
    }

    [Fact] // P0-4: 13o proporcional da rescisao tem INSS-13/IRRF-13 em BASE PROPRIA (base separada).
    public async Task Rescisao_13_proporcional_apura_inss_irrf_em_base_separada()
    {
        Guid servidorId;
        var competencia = Competencia.De(2026, 12);

        await using (var ctx = CriarContexto(TenantA))
        {
            servidorId = await SemearServidorAsync(ctx, RegimePrevidenciario.Rgps);
            SemearTabelas(ctx);
            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("13-PROP"), "13o Proporcional", NaturezaRubrica.Provento, competencia, incideInss: true, incideIrrf: true));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            // Admissao 2026-01-05, desligamento 2026-12-20 -> 12 avos; vencimento 5000 -> 13-prop = 5000.
            await HandlerRescisao(ctx).Handle(new GerarVerbasRescisoriasCommand(
                servidorId,
                DataDesligamento: new DateOnly(2026, 12, 20),
                TipoDesligamento: TipoDesligamento.Aposentadoria,
                Regime: RegimeVinculo.Estatutario,
                Vencimento: 5000m,
                DiasTrabalhadosNoMes: 20,
                Admissao: new DateOnly(2026, 1, 5),
                InicioPeriodoAquisitivoFerias: new DateOnly(2026, 1, 5),
                DiasFeriasVencidas: 0,
                ValorAvisoPrevio: 0m,
                ValorMultaFgts: 0m), default);

            var folha = await ctx.FolhasDePagamento.Include(f => f.Eventos).SingleAsync(f => f.Tipo == TipoFolha.Rescisao);
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "13-PROP" && e.Valor == 5000m);
            // INSS-13 sobre 5000 = 501,51; IRRF-13 base separada (sem simplificado) = 336,67 — mesmos do 13o anual.
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "INSS-13" && e.Valor == 501.51m);
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "IRRF-13" && e.Valor == 336.67m);
        }
    }

    [Fact] // Coexistencia: folha mensal e folha de 13o na MESMA competencia (indice por Tipo).
    public async Task Folha_mensal_e_decimo_terceiro_coexistem_na_mesma_competencia()
    {
        Guid servidorId;
        var competencia = Competencia.De(2026, 12);

        await using (var ctx = CriarContexto(TenantA))
        {
            servidorId = await SemearServidorAsync(ctx, RegimePrevidenciario.Rgps);
            SemearTabelas(ctx);
            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("13-SAL"), "13o", NaturezaRubrica.Provento, competencia, incideInss: true, incideIrrf: true));

            var mensal = FolhaDePagamento.Abrir(TenantA, competencia);
            ctx.FolhasDePagamento.Add(mensal);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            await Handler13(ctx).Handle(
                new GerarDecimoTerceiroCommand(servidorId, 2026, 12, 1, 5000m, new DateOnly(2026, 1, 5)), default);

            var folhas = await ctx.FolhasDePagamento.Where(f => f.Competencia == competencia).ToListAsync();
            folhas.Should().HaveCount(2);
            folhas.Select(f => f.Tipo).Should().Contain(new[] { TipoFolha.Mensal, TipoFolha.DecimoTerceiro });
        }
    }

    [Fact] // P0-4 regressao: ApurarDescontosLegais sobre folha de rescisao (com INSS-13 ja lancado em base
            // separada) NAO pode falhar fail-closed nem re-tributar o 13o; apura so as verbas MENSAIS (saldo).
    public async Task ApuracaoLegal_sobre_rescisao_ignora_descontos_de_base_separada_do_13o()
    {
        Guid servidorId;
        Guid folhaId;
        var competencia = Competencia.De(2026, 12);

        await using (var ctx = CriarContexto(TenantA))
        {
            servidorId = await SemearServidorAsync(ctx, RegimePrevidenciario.Rgps);
            SemearTabelas(ctx);
            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("13-PROP"), "13o Proporcional", NaturezaRubrica.Provento, competencia, incideInss: true, incideIrrf: true));
            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("SALDO-SAL"), "Saldo Salario", NaturezaRubrica.Provento, competencia, incideInss: true, incideIrrf: true));
            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("FERIAS-VENC"), "Ferias Vencidas", NaturezaRubrica.Provento, competencia));
            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("FERIAS-PROP"), "Ferias Prop", NaturezaRubrica.Provento, competencia));
            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("1/3-FERIAS"), "Terco", NaturezaRubrica.Provento, competencia));
            await ctx.SaveChangesAsync();
        }

        // Gera a rescisao: saldo (mensal, tributavel) + 13o-prop (base separada -> INSS-13/IRRF-13).
        await using (var ctx = CriarContexto(TenantA))
        {
            await HandlerRescisao(ctx).Handle(new GerarVerbasRescisoriasCommand(
                servidorId,
                DataDesligamento: new DateOnly(2026, 12, 20),
                TipoDesligamento: TipoDesligamento.Aposentadoria,
                Regime: RegimeVinculo.Estatutario,
                Vencimento: 5000m,
                DiasTrabalhadosNoMes: 30,
                Admissao: new DateOnly(2026, 1, 5),
                InicioPeriodoAquisitivoFerias: new DateOnly(2026, 1, 5),
                DiasFeriasVencidas: 0,
                ValorAvisoPrevio: 0m,
                ValorMultaFgts: 0m), default);
            var folha = await ctx.FolhasDePagamento.SingleAsync(f => f.Tipo == TipoFolha.Rescisao);
            folhaId = folha.Id.Value;
        }

        // Apura os descontos legais MENSAIS sobre a mesma folha: nao deve lancar excecao (a folha ja tem INSS-13).
        await using (var ctx = CriarContexto(TenantA))
        {
            var apurar = new ApurarDescontosLegaisHandler(
                new FolhaDePagamentoRepository(ctx), new RubricaFolhaRepository(ctx),
                new ServidorRegimeConsulta(ctx), new TabelasLegaisProvider(ctx),
                new ParametrosFolhaProviderFake(), ctx);

            var acao = async () => await apurar.Handle(new ApurarDescontosLegaisCommand(folhaId), default);
            await acao.Should().NotThrowAsync();

            var folha = await ctx.FolhasDePagamento.Include(f => f.Eventos).SingleAsync(f => f.Id == new FolhaDePagamentoId(folhaId));
            // Base separada do 13o preservada (INSS-13) E base mensal apurada (INSS) — DUAS bases distintas,
            // cada uma com a SUA propria base de INSS (o 13o nunca foi somado a base mensal nem re-tributado).
            var inss13 = folha.Eventos.Single(e => e.Rubrica.Codigo == "INSS-13");
            var inssMensal = folha.Eventos.Single(e => e.Rubrica.Codigo == "INSS");
            inss13.BaseCalculo.Valor.Should().Be(5000m); // base do 13o-prop, isolada (art. 12-A).
            // A base mensal do INSS NAO inclui o 13o (5000): se incluisse, a base seria 5000 maior.
            inssMensal.BaseCalculo.Valor.Should().NotBe(inss13.BaseCalculo.Valor + 5000m);
        }
    }
}
