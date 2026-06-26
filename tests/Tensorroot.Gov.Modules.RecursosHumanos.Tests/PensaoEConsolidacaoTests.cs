using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.TabelasLegais;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
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
/// Cobertura dos bugs de calculo P0: pensao alimenticia ligada ao motor (P0-1), consolidacao fiscal de
/// multiplas folhas na mesma competencia (P0-2) e sinalizacao de liquido insuficiente (P0-5). Integra
/// sobre SQLite em memoria, com tabelas federais reais e catalogo de rubricas vigente.
/// </summary>
public sealed class PensaoEConsolidacaoTests : RecursosHumanosTestBase
{
    private sealed class ParametrosFolhaProviderFake : IParametrosFolhaProvider
    {
        public Task<ParametrosFolha> ObterAsync(CancellationToken cancellationToken)
            => Task.FromResult(new ParametrosFolha { TetoRemuneratorio = 50000m });
    }

    private static void SemearTabelasERubricas(RecursosHumanosDbContext ctx, Competencia competencia)
    {
        foreach (var t in TabelasFederaisSeed.Inss(TenantA))
        {
            ctx.TabelasInss.Add(t);
        }

        foreach (var t in TabelasFederaisSeed.Irrf(TenantA))
        {
            ctx.TabelasIrrf.Add(t);
        }

        ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("VENCIMENTO"), "Vencimento", NaturezaRubrica.Provento, competencia, incideInss: true, incideIrrf: true));
    }

    private static ApurarDescontosLegaisHandler Apurar(RecursosHumanosDbContext ctx)
        => new(
            new FolhaDePagamentoRepository(ctx), new RubricaFolhaRepository(ctx),
            new ServidorRegimeConsulta(ctx), new TabelasLegaisProvider(ctx),
            new ParametrosFolhaProviderFake(), ctx);

    // ---------- P0-1: pensao alimenticia ----------

    [Fact] // Pensao por VALOR FIXO: deduz do IRRF e gera o desconto/repasse (nunca mais zero hardcoded).
    public async Task Pensao_valor_fixo_deduz_irrf_e_gera_desconto()
    {
        Guid folhaId;
        var competencia = Competencia.De(2026, 6);

        await using (var ctx = CriarContexto(TenantA))
        {
            var servidor = Servidor.Admitir(
                TenantA, Cpf.Create("52998224725"), Matricula.De("M-PEN"),
                DadosPessoais.Criar("Servidor Com Pensao", new DateOnly(1990, 1, 1)),
                CargoId.New(), RegimePrevidenciario.Rgps, new DateOnly(2026, 1, 5));
            servidor.AdicionarPensaoAlimenticia(PensaoAlimenticia.PorValorFixo("Filho Menor", 800m, "0001-2026"));
            ctx.Servidores.Add(servidor);
            SemearTabelasERubricas(ctx, competencia);

            // Base R$ 7.000 (faixa decrescente do redutor 2026): o IRRF permanece POSITIVO mesmo apos o redutor
            // parcial, deixando observavel a deducao da pensao (em R$ 5.000 o redutor zeraria o IRRF).
            var folha = FolhaDePagamento.Abrir(TenantA, competencia);
            folha.AdicionarEvento(servidor.Id.Value, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(7000m), 7000m, RegimePrevidenciario.Rgps);
            folhaId = folha.Id.Value;
            ctx.FolhasDePagamento.Add(folha);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            await Apurar(ctx).Handle(new ApurarDescontosLegaisCommand(folhaId), default);

            var folha = await ctx.FolhasDePagamento.Include(f => f.Eventos).SingleAsync(f => f.Id == new FolhaDePagamentoId(folhaId));
            // Pensao gera o desconto/repasse de 800.
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "PENSAO-ALIM" && e.Valor == 800m);
            // IRRF deduz a pensao: base de IRRF cai 800 ante o cenario sem pensao (802,68 em R$ 7.000) -> imposto menor e positivo.
            var irrf = folha.Eventos.Single(e => e.Rubrica.Codigo == "IRRF");
            irrf.Valor.Should().BeLessThan(802.68m);
            irrf.Valor.Should().BeGreaterThan(0m);
        }
    }

    [Fact] // Pensao por PERCENTUAL sobre liquido (proventos - previdencia): 30% de (5000 - 501,51).
    public async Task Pensao_percentual_sobre_liquido_apura_corretamente()
    {
        Guid folhaId;
        Guid servidorId;
        var competencia = Competencia.De(2026, 6);

        await using (var ctx = CriarContexto(TenantA))
        {
            var servidor = Servidor.Admitir(
                TenantA, Cpf.Create("52998224725"), Matricula.De("M-PEN2"),
                DadosPessoais.Criar("Servidor Pensao Percentual", new DateOnly(1990, 1, 1)),
                CargoId.New(), RegimePrevidenciario.Rgps, new DateOnly(2026, 1, 5));
            servidor.AdicionarPensaoAlimenticia(PensaoAlimenticia.PorPercentual("Ex-Conjuge", 0.30m, BasePensao.LiquidoAposDescontosLegais, "0002-2026"));
            servidorId = servidor.Id.Value;
            ctx.Servidores.Add(servidor);
            SemearTabelasERubricas(ctx, competencia);

            var folha = FolhaDePagamento.Abrir(TenantA, competencia);
            folha.AdicionarEvento(servidorId, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rgps);
            folhaId = folha.Id.Value;
            ctx.FolhasDePagamento.Add(folha);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            await Apurar(ctx).Handle(new ApurarDescontosLegaisCommand(folhaId), default);

            var folha = await ctx.FolhasDePagamento.Include(f => f.Eventos).SingleAsync(f => f.Id == new FolhaDePagamentoId(folhaId));
            // INSS sobre 5000 = 501,51. Base liquida = 5000 - 501,51 = 4498,49. 30% = 1349,55.
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "PENSAO-ALIM" && e.Valor == 1349.55m);
        }
    }

    // ---------- P0-5: liquido insuficiente ----------

    [Fact] // Descontos manuais excedem proventos -> folha MARCADA (nao zera em silencio) e fechamento bloqueado.
    public async Task Liquido_insuficiente_sinaliza_e_bloqueia_fechamento()
    {
        Guid folhaId;
        var competencia = Competencia.De(2026, 6);

        await using (var ctx = CriarContexto(TenantA))
        {
            var servidor = Servidor.Admitir(
                TenantA, Cpf.Create("52998224725"), Matricula.De("M-NEG"),
                DadosPessoais.Criar("Servidor Liquido Negativo", new DateOnly(1990, 1, 1)),
                CargoId.New(), RegimePrevidenciario.Rgps, new DateOnly(2026, 1, 5));
            ctx.Servidores.Add(servidor);
            SemearTabelasERubricas(ctx, competencia);
            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("CONSIG"), "Consignado", NaturezaRubrica.Desconto, competencia));

            var folha = FolhaDePagamento.Abrir(TenantA, competencia);
            folha.AdicionarEvento(servidor.Id.Value, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(2000m), 2000m, RegimePrevidenciario.Rgps);
            // Consignado de 3000 > proventos -> liquido insuficiente.
            folha.AdicionarEvento(servidor.Id.Value, Rubrica.De("CONSIG"), TipoEvento.Desconto, BaseCalculo.De(3000m), 3000m, RegimePrevidenciario.Rgps);
            folhaId = folha.Id.Value;
            ctx.FolhasDePagamento.Add(folha);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var folha = await ctx.FolhasDePagamento.Include(f => f.Eventos).SingleAsync(f => f.Id == new FolhaDePagamentoId(folhaId));
            folha.Calcular(50000m, new DateOnly(2026, 7, 1));

            folha.TemLiquidoInsuficiente.Should().BeTrue();
            folha.ServidoresComLiquidoInsuficiente.Should().HaveCount(1);
            folha.TotalLiquido.Valor.Should().Be(0m); // total nao-negativo, mas MARCADO.

            // Fechamento sem confirmacao explicita e RECUSADO (P0-5: nao fecha em silencio).
            var acao = () => folha.Fechar(new DateOnly(2026, 7, 5));
            acao.Should().Throw<InvalidOperationException>().WithMessage("*liquido insuficiente*");

            // Com confirmacao explicita (revisao feita), fecha.
            folha.Fechar(new DateOnly(2026, 7, 5), confirmarLiquidoInsuficiente: true);
            folha.Situacao.Should().Be(SituacaoFolha.Fechada);
        }
    }

    // ---------- P0-2: consolidacao multi-folha no mes ----------

    [Fact] // Mensal + Ferias na mesma competencia: INSS/IRRF sobre a SOMA (teto INSS unico, faixa IRRF correta).
    public async Task Consolidacao_mensal_e_ferias_apura_inss_irrf_sobre_a_soma()
    {
        Guid servidorId;
        var competencia = Competencia.De(2026, 6);

        await using (var ctx = CriarContexto(TenantA))
        {
            var servidor = Servidor.Admitir(
                TenantA, Cpf.Create("52998224725"), Matricula.De("M-CONS"),
                DadosPessoais.Criar("Servidor Consolidacao", new DateOnly(1990, 1, 1)),
                CargoId.New(), RegimePrevidenciario.Rgps, new DateOnly(2026, 1, 5));
            servidorId = servidor.Id.Value;
            ctx.Servidores.Add(servidor);
            SemearTabelasERubricas(ctx, competencia);

            // Folha mensal: 5000.
            var mensal = FolhaDePagamento.Abrir(TenantA, competencia, TipoFolha.Mensal);
            mensal.AdicionarEvento(servidorId, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rgps);
            ctx.FolhasDePagamento.Add(mensal);

            // Folha de ferias: mais 5000 (mesma rubrica VENCIMENTO, incide INSS/IRRF) na MESMA competencia.
            var ferias = FolhaDePagamento.Abrir(TenantA, competencia, TipoFolha.Ferias);
            ferias.AdicionarEvento(servidorId, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rgps);
            ctx.FolhasDePagamento.Add(ferias);

            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new ConsolidarDescontosLegaisMensaisHandler(
                new FolhaDePagamentoRepository(ctx), new RubricaFolhaRepository(ctx),
                new ServidorRegimeConsulta(ctx), new TabelasLegaisProvider(ctx),
                new ParametrosFolhaProviderFake(), ctx);

            await handler.Handle(new ConsolidarDescontosLegaisMensaisCommand(2026, 6), default);

            var folhas = await ctx.FolhasDePagamento.Include(f => f.Eventos).Where(f => f.Competencia == competencia).ToListAsync();
            var todosEventos = folhas.SelectMany(f => f.Eventos).ToList();

            // INSS consolidado sobre 10000 atinge o TETO unico (8.475,55) -> 988,09 (nao 2x 501,51 = 1003,02).
            var inss = todosEventos.Where(e => e.Rubrica.Codigo == "INSS").Sum(e => e.Valor);
            inss.Should().Be(988.09m);

            // IRRF consolidado sobre 10000 cai na faixa de 27,5% — bem acima de 2x o IRRF de 5000 isolado.
            var irrf = todosEventos.Where(e => e.Rubrica.Codigo == "IRRF").Sum(e => e.Valor);
            irrf.Should().BeGreaterThan(2 * 312.89m);

            // Nao duplica: o desconto legal e concentrado na folha principal (Mensal).
            var mensal = folhas.Single(f => f.Tipo == TipoFolha.Mensal);
            var ferias = folhas.Single(f => f.Tipo == TipoFolha.Ferias);
            mensal.Eventos.Count(e => e.Rubrica.Codigo == "INSS").Should().Be(1);
            ferias.Eventos.Should().NotContain(e => e.Rubrica.Codigo == "INSS");
        }
    }

    [Fact] // RH-PLANO §97: ISOLADO (errado) vs CONSOLIDADO (correto), MESMA competencia, com NUMEROS.
    public async Task Isolado_subtributa_versus_consolidado_correto_inss_e_irrf()
    {
        // Cenario do P0-2: servidor RGPS com salario 5000 recebe TAMBEM 5000 em folha de ferias na MESMA
        // competencia. Apurar cada folha ISOLADA (comportamento do bug) subtributa INSS (cada parcela
        // abaixo do teto) e IRRF (cada parcela em faixa menor). O consolidado tributa sobre a SOMA (10000).
        var competencia = Competencia.De(2026, 6);

        // ---------- (A) ISOLADO: apura cada folha em separado (reproduz o bug) ----------
        Guid mensalIsoladaId;
        Guid feriasIsoladaId;
        await using (var ctx = CriarContexto(TenantA))
        {
            var servidor = Servidor.Admitir(
                TenantA, Cpf.Create("52998224725"), Matricula.De("M-ISO"),
                DadosPessoais.Criar("Servidor Isolado", new DateOnly(1990, 1, 1)),
                CargoId.New(), RegimePrevidenciario.Rgps, new DateOnly(2026, 1, 5));
            ctx.Servidores.Add(servidor);
            SemearTabelasERubricas(ctx, competencia);

            var mensal = FolhaDePagamento.Abrir(TenantA, competencia, TipoFolha.Mensal);
            mensal.AdicionarEvento(servidor.Id.Value, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rgps);
            mensalIsoladaId = mensal.Id.Value;
            ctx.FolhasDePagamento.Add(mensal);

            var ferias = FolhaDePagamento.Abrir(TenantA, competencia, TipoFolha.Ferias);
            ferias.AdicionarEvento(servidor.Id.Value, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rgps);
            feriasIsoladaId = ferias.Id.Value;
            ctx.FolhasDePagamento.Add(ferias);
            await ctx.SaveChangesAsync();
        }

        decimal inssIsolado;
        decimal irrfIsolado;
        await using (var ctx = CriarContexto(TenantA))
        {
            await Apurar(ctx).Handle(new ApurarDescontosLegaisCommand(mensalIsoladaId), default);
            await Apurar(ctx).Handle(new ApurarDescontosLegaisCommand(feriasIsoladaId), default);

            var folhas = await ctx.FolhasDePagamento.Include(f => f.Eventos).Where(f => f.Competencia == competencia).ToListAsync();
            var eventos = folhas.SelectMany(f => f.Eventos).ToList();
            inssIsolado = eventos.Where(e => e.Rubrica.Codigo == "INSS").Sum(e => e.Valor);
            irrfIsolado = eventos.Where(e => e.Rubrica.Codigo == "IRRF").Sum(e => e.Valor);
        }

        // INSS isolado = 2 x 501,51 = 1003,02 (cada parcela de 5000 abaixo do teto). ERRADO: ignora o teto unico.
        inssIsolado.Should().Be(1003.02m);
        // IRRF isolado (2026) = 0: cada parcela de 5000, apurada isolada, fica isenta pelo redutor mensal da
        // Lei 15.270/2025 (isencao efetiva ate 5.000). ERRADO: ignora a progressividade — a SOMA (10000) e tributada.
        irrfIsolado.Should().Be(0m);

        // ---------- (B) CONSOLIDADO: re-apura sobre a SOMA (10000) ----------
        // Competencia distinta (a constraint UNIQUE e por TenantId+Competencia+Tipo); tabelas vigentes em
        // 2026 (mesmos parametros legais), entao os numeros do consolidado sao comparaveis ao isolado.
        var competenciaCons = Competencia.De(2026, 7);
        Guid mensalConsId;
        Guid feriasConsId;
        await using (var ctx = CriarContexto(TenantA))
        {
            var servidor = Servidor.Admitir(
                TenantA, Cpf.Create("15350946056"), Matricula.De("M-CON2"),
                DadosPessoais.Criar("Servidor Consolidado", new DateOnly(1990, 1, 1)),
                CargoId.New(), RegimePrevidenciario.Rgps, new DateOnly(2026, 1, 5));
            ctx.Servidores.Add(servidor);
            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("VENC2"), "Vencimento Jul", NaturezaRubrica.Provento, competenciaCons, incideInss: true, incideIrrf: true));

            var mensal = FolhaDePagamento.Abrir(TenantA, competenciaCons, TipoFolha.Mensal);
            mensal.AdicionarEvento(servidor.Id.Value, Rubrica.De("VENC2"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rgps);
            mensalConsId = mensal.Id.Value;
            ctx.FolhasDePagamento.Add(mensal);

            var ferias = FolhaDePagamento.Abrir(TenantA, competenciaCons, TipoFolha.Ferias);
            ferias.AdicionarEvento(servidor.Id.Value, Rubrica.De("VENC2"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rgps);
            feriasConsId = ferias.Id.Value;
            ctx.FolhasDePagamento.Add(ferias);
            await ctx.SaveChangesAsync();
        }

        decimal inssConsolidado;
        decimal irrfConsolidado;
        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new ConsolidarDescontosLegaisMensaisHandler(
                new FolhaDePagamentoRepository(ctx), new RubricaFolhaRepository(ctx),
                new ServidorRegimeConsulta(ctx), new TabelasLegaisProvider(ctx),
                new ParametrosFolhaProviderFake(), ctx);
            await handler.Handle(new ConsolidarDescontosLegaisMensaisCommand(2026, 7), default);

            var folhas = await ctx.FolhasDePagamento.Include(f => f.Eventos)
                .Where(f => f.Id == new FolhaDePagamentoId(mensalConsId) || f.Id == new FolhaDePagamentoId(feriasConsId))
                .ToListAsync();
            var eventos = folhas.SelectMany(f => f.Eventos).ToList();
            inssConsolidado = eventos.Where(e => e.Rubrica.Codigo == "INSS").Sum(e => e.Valor);
            irrfConsolidado = eventos.Where(e => e.Rubrica.Codigo == "IRRF").Sum(e => e.Valor);
        }

        // INSS consolidado atinge o TETO unico de 2026 (base 8.475,55) = 988,09. MENOR que o isolado:
        // o teto limita a contribuicao, mas o isolado recolheu sobre as duas parcelas sem teto.
        inssConsolidado.Should().Be(988.09m);
        // IRRF consolidado sobre 10000 cai na faixa de 27,5% e e MAIOR que o isolado (subtributacao do bug).
        irrfConsolidado.Should().BeGreaterThan(irrfIsolado);

        // A divergencia e material: o isolado NAO bate com o consolidado em nenhum dos dois tributos.
        inssIsolado.Should().NotBe(inssConsolidado);
        irrfIsolado.Should().NotBe(irrfConsolidado);
    }

    [Fact] // Consolidacao idempotente: rodar duas vezes nao duplica os descontos legais.
    public async Task Consolidacao_e_idempotente()
    {
        Guid servidorId;
        var competencia = Competencia.De(2026, 6);

        await using (var ctx = CriarContexto(TenantA))
        {
            var servidor = Servidor.Admitir(
                TenantA, Cpf.Create("52998224725"), Matricula.De("M-IDEM"),
                DadosPessoais.Criar("Servidor Idem", new DateOnly(1990, 1, 1)),
                CargoId.New(), RegimePrevidenciario.Rgps, new DateOnly(2026, 1, 5));
            servidorId = servidor.Id.Value;
            ctx.Servidores.Add(servidor);
            SemearTabelasERubricas(ctx, competencia);

            var mensal = FolhaDePagamento.Abrir(TenantA, competencia, TipoFolha.Mensal);
            mensal.AdicionarEvento(servidorId, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rgps);
            ctx.FolhasDePagamento.Add(mensal);

            var ferias = FolhaDePagamento.Abrir(TenantA, competencia, TipoFolha.Ferias);
            ferias.AdicionarEvento(servidorId, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rgps);
            ctx.FolhasDePagamento.Add(ferias);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new ConsolidarDescontosLegaisMensaisHandler(
                new FolhaDePagamentoRepository(ctx), new RubricaFolhaRepository(ctx),
                new ServidorRegimeConsulta(ctx), new TabelasLegaisProvider(ctx),
                new ParametrosFolhaProviderFake(), ctx);

            await handler.Handle(new ConsolidarDescontosLegaisMensaisCommand(2026, 6), default);
            await handler.Handle(new ConsolidarDescontosLegaisMensaisCommand(2026, 6), default);

            var folhas = await ctx.FolhasDePagamento.Include(f => f.Eventos).Where(f => f.Competencia == competencia).ToListAsync();
            folhas.SelectMany(f => f.Eventos).Count(e => e.Rubrica.Codigo == "INSS").Should().Be(1);
            folhas.SelectMany(f => f.Eventos).Count(e => e.Rubrica.Codigo == "IRRF").Should().Be(1);
        }
    }
}
