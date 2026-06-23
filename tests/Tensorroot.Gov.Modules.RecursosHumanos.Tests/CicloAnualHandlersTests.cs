using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.CicloAnual;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.TabelasLegais;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.CicloAnual;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
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

    [Fact] // Rescisao estatutaria (exoneracao): saldo + 13o prop + ferias vencidas + 1/3; SEM aviso/multa.
    public async Task Rescisao_estatutaria_compoe_verbas_sem_aviso_nem_multa()
    {
        Guid servidorId;

        await using (var ctx = CriarContexto(TenantA))
        {
            servidorId = await SemearServidorAsync(ctx, RegimePrevidenciario.Rpps);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new GerarVerbasRescisoriasHandler(
                new FolhaDePagamentoRepository(ctx), new ServidorRegimeConsulta(ctx),
                new ParametrosFolhaProviderFake(), ctx, new TenantContextFake(TenantA));

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
}
