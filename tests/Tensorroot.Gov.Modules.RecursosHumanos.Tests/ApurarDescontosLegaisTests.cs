using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.TabelasLegais;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Calculo;
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
/// Cobertura de integracao do caso de uso <see cref="ApurarDescontosLegaisHandler"/>: monta uma folha
/// com proventos lancados, resolve as incidencias pelo catalogo de rubricas, apura INSS/IRRF pelo
/// motor com as tabelas parametrizadas e confere os descontos legais lancados e o liquido final.
/// </summary>
public sealed class ApurarDescontosLegaisTests : RecursosHumanosTestBase
{
    private sealed class ParametrosFolhaProviderFake : IParametrosFolhaProvider
    {
        public Task<ParametrosFolha> ObterAsync(CancellationToken cancellationToken)
            => Task.FromResult(new ParametrosFolha { TetoRemuneratorio = 50000m });
    }

    private static async Task<Guid> SemearServidorAsync(RecursosHumanosDbContext ctx, RegimePrevidenciario regime, int dependentes)
    {
        var servidor = Servidor.Admitir(
            TenantA,
            Cpf.Create("52998224725"),
            Matricula.De("M-0001"),
            DadosPessoais.Criar("Fulano de Tal", new DateOnly(1990, 1, 1)),
            CargoId.New(),
            regime,
            new DateOnly(2026, 1, 5));
        for (var i = 0; i < dependentes; i++)
        {
            // Filhos menores: elegiveis a deducao de IRRF (Lei 9.250/1995 art. 35).
            servidor.AdicionarDependente($"Dep {i}", "Filho", new DateOnly(2015, 1, 1), elegivelIrrf: true);
        }

        ctx.Servidores.Add(servidor);
        await ctx.SaveChangesAsync();
        return servidor.Id.Value;
    }

    /// <summary>
    /// RH-D1: semeia um servidor RGPS com um unico CPF parametrizavel e um unico dependente cuja
    /// elegibilidade ao IRRF (rol fechado da Lei 9.250/1995 art. 35) e explicitamente informada.
    /// </summary>
    private static async Task<Guid> SemearServidorComDependenteAsync(
        RecursosHumanosDbContext ctx,
        string cpf,
        string matricula,
        bool dependenteElegivelIrrf)
    {
        var servidor = Servidor.Admitir(
            TenantA,
            Cpf.Create(cpf),
            Matricula.De(matricula),
            DadosPessoais.Criar("Servidor RH-D1", new DateOnly(1985, 3, 10)),
            CargoId.New(),
            RegimePrevidenciario.Rgps,
            new DateOnly(2026, 1, 5));
        // Dependente registrado para BENEFICIOS (ex.: sogra/maior nao-estudante): NAO elegivel ao IRRF;
        // ou dependente elegivel (filho menor). A elegibilidade e o unico fator que muda entre os cenarios.
        servidor.AdicionarDependente("Dependente", "Sogra", new DateOnly(1960, 1, 1), elegivelIrrf: dependenteElegivelIrrf);

        ctx.Servidores.Add(servidor);
        await ctx.SaveChangesAsync();
        return servidor.Id.Value;
    }

    /// <summary>
    /// RH-D1 (DENY): a deducao por dependente do IRRF conta SOMENTE dependentes ELEGIVEIS (rol fechado da
    /// Lei 9.250/1995 art. 35). Prova: dois servidores na MESMA folha com a MESMA base (5000), um com 1
    /// dependente NAO-elegivel (registrado so para beneficios) e outro com 1 dependente ELEGIVEL. O
    /// nao-elegivel NAO pode reduzir a base do IRRF — seu imposto deve ser igual ao baseline de zero
    /// dependentes (312,89) e estritamente MAIOR que o do servidor com dependente elegivel. Sem o fix
    /// (Dependentes.Count cru) ambos deduziriam e haveria sub-recolhimento de IRRF na fonte.
    /// </summary>
    [Fact]
    public async Task Dependente_nao_elegivel_nao_reduz_base_do_irrf()
    {
        var competencia = Competencia.De(2026, 6);
        Guid folhaId;
        Guid idNaoElegivel;
        Guid idElegivel;

        await using (var ctx = CriarContexto(TenantA))
        {
            // CPFs validos e distintos (matriculas distintas) para os dois servidores.
            idNaoElegivel = await SemearServidorComDependenteAsync(ctx, "52998224725", "M-NE-01", dependenteElegivelIrrf: false);
            idElegivel = await SemearServidorComDependenteAsync(ctx, "11144477735", "M-EL-01", dependenteElegivelIrrf: true);

            foreach (var t in TabelasFederaisSeed.Inss(TenantA))
            {
                ctx.TabelasInss.Add(t);
            }

            foreach (var t in TabelasFederaisSeed.Irrf(TenantA))
            {
                ctx.TabelasIrrf.Add(t);
            }

            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("VENCIMENTO"), "Vencimento", NaturezaRubrica.Provento, competencia, incideInss: true, incideIrrf: true));

            // Uma unica folha Mensal (invariante: unica por Tenant+Competencia+Tipo) com os dois servidores.
            var folha = FolhaDePagamento.Abrir(TenantA, competencia);
            folha.AdicionarEvento(idNaoElegivel, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rgps);
            folha.AdicionarEvento(idElegivel, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rgps);
            folhaId = folha.Id.Value;
            ctx.FolhasDePagamento.Add(folha);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new ApurarDescontosLegaisHandler(
                new FolhaDePagamentoRepository(ctx),
                new RubricaFolhaRepository(ctx),
                new ServidorRegimeConsulta(ctx),
                new TabelasLegaisProvider(ctx),
                new ParametrosFolhaProviderFake(),
                ctx);

            await handler.Handle(new ApurarDescontosLegaisCommand(folhaId), default);

            var folha = await ctx.FolhasDePagamento.Include(f => f.Eventos)
                .SingleAsync(f => f.Id == new FolhaDePagamentoId(folhaId));

            var irrfNaoElegivel = folha.Eventos.Single(e => e.ServidorId == idNaoElegivel && e.Rubrica.Codigo == "IRRF").Valor;
            var irrfElegivel = folha.Eventos.Single(e => e.ServidorId == idElegivel && e.Rubrica.Codigo == "IRRF").Valor;

            // O dependente NAO-elegivel nao deduz: imposto identico ao baseline de zero dependentes (312,89).
            irrfNaoElegivel.Should().Be(312.89m);
            // O dependente ELEGIVEL deduz 189,59 da base -> imposto estritamente menor.
            irrfElegivel.Should().BeLessThan(irrfNaoElegivel);
        }
    }

    [Fact] // RGPS 5000, 0 dependentes: apura INSS 501,51 + IRRF 312,89; liquido 4185,60.
    public async Task Apura_descontos_legais_rgps_e_calcula_liquido()
    {
        Guid folhaId;
        Guid servidorId;
        var competencia = Competencia.De(2026, 6);

        await using (var ctx = CriarContexto(TenantA))
        {
            servidorId = await SemearServidorAsync(ctx, RegimePrevidenciario.Rgps, 0);

            foreach (var t in TabelasFederaisSeed.Inss(TenantA))
            {
                ctx.TabelasInss.Add(t);
            }

            foreach (var t in TabelasFederaisSeed.Irrf(TenantA))
            {
                ctx.TabelasIrrf.Add(t);
            }

            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("VENCIMENTO"), "Vencimento", NaturezaRubrica.Provento, competencia, incideInss: true, incideIrrf: true));

            var folha = FolhaDePagamento.Abrir(TenantA, competencia);
            folha.AdicionarEvento(servidorId, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rgps);
            folhaId = folha.Id.Value;
            ctx.FolhasDePagamento.Add(folha);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new ApurarDescontosLegaisHandler(
                new FolhaDePagamentoRepository(ctx),
                new RubricaFolhaRepository(ctx),
                new ServidorRegimeConsulta(ctx),
                new TabelasLegaisProvider(ctx),
                new ParametrosFolhaProviderFake(),
                ctx);

            await handler.Handle(new ApurarDescontosLegaisCommand(folhaId), default);

            var folha = await ctx.FolhasDePagamento.Include(f => f.Eventos).SingleAsync(f => f.Id == new FolhaDePagamentoId(folhaId));
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "INSS" && e.Valor == 501.51m);
            folha.Eventos.Should().Contain(e => e.Rubrica.Codigo == "IRRF" && e.Valor == 312.89m);

            folha.Calcular(50000m, new DateOnly(2026, 7, 1));
            folha.TotalLiquido.Valor.Should().Be(4185.60m);
        }
    }

    [Fact] // Re-apuracao idempotente: rodar duas vezes nao duplica os descontos legais.
    public async Task Reapuracao_e_idempotente()
    {
        Guid folhaId;
        var competencia = Competencia.De(2026, 6);

        await using (var ctx = CriarContexto(TenantA))
        {
            var servidorId = await SemearServidorAsync(ctx, RegimePrevidenciario.Rgps, 0);
            foreach (var t in TabelasFederaisSeed.Inss(TenantA))
            {
                ctx.TabelasInss.Add(t);
            }

            foreach (var t in TabelasFederaisSeed.Irrf(TenantA))
            {
                ctx.TabelasIrrf.Add(t);
            }

            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("VENCIMENTO"), "Vencimento", NaturezaRubrica.Provento, competencia, incideInss: true, incideIrrf: true));
            var folha = FolhaDePagamento.Abrir(TenantA, competencia);
            folha.AdicionarEvento(servidorId, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rgps);
            folhaId = folha.Id.Value;
            ctx.FolhasDePagamento.Add(folha);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new ApurarDescontosLegaisHandler(
                new FolhaDePagamentoRepository(ctx), new RubricaFolhaRepository(ctx),
                new ServidorRegimeConsulta(ctx), new TabelasLegaisProvider(ctx),
                new ParametrosFolhaProviderFake(), ctx);

            await handler.Handle(new ApurarDescontosLegaisCommand(folhaId), default);
            await handler.Handle(new ApurarDescontosLegaisCommand(folhaId), default);

            var folha = await ctx.FolhasDePagamento.Include(f => f.Eventos).SingleAsync(f => f.Id == new FolhaDePagamentoId(folhaId));
            folha.Eventos.Count(e => e.Rubrica.Codigo == "INSS").Should().Be(1);
            folha.Eventos.Count(e => e.Rubrica.Codigo == "IRRF").Should().Be(1);
        }
    }

    [Fact] // Fail-closed: servidor efetivo (RPPS) sem tabela municipal impede a apuracao.
    public async Task Apuracao_rpps_sem_tabela_municipal_falha()
    {
        Guid folhaId;
        var competencia = Competencia.De(2026, 6);

        await using (var ctx = CriarContexto(TenantA))
        {
            var servidorId = await SemearServidorAsync(ctx, RegimePrevidenciario.Rpps, 0);
            foreach (var t in TabelasFederaisSeed.Irrf(TenantA))
            {
                ctx.TabelasIrrf.Add(t);
            }

            ctx.Rubricas.Add(RubricaFolha.Criar(TenantA, Rubrica.De("VENCIMENTO"), "Vencimento", NaturezaRubrica.Provento, competencia, incideRpps: true, incideIrrf: true));
            var folha = FolhaDePagamento.Abrir(TenantA, competencia);
            folha.AdicionarEvento(servidorId, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rpps);
            folhaId = folha.Id.Value;
            ctx.FolhasDePagamento.Add(folha);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new ApurarDescontosLegaisHandler(
                new FolhaDePagamentoRepository(ctx), new RubricaFolhaRepository(ctx),
                new ServidorRegimeConsulta(ctx), new TabelasLegaisProvider(ctx),
                new ParametrosFolhaProviderFake(), ctx);

            var acao = async () => await handler.Handle(new ApurarDescontosLegaisCommand(folhaId), default);
            await acao.Should().ThrowAsync<CalculoFolhaException>();
        }
    }

    [Fact] // F-A3: provento com rubrica FORA de vigencia na competencia -> FAIL-CLOSED (nao presume base zero).
    public async Task Provento_com_rubrica_fora_de_vigencia_falha_em_vez_de_presumir_base_zero()
    {
        Guid folhaId;
        var competencia = Competencia.De(2026, 6);

        await using (var ctx = CriarContexto(TenantA))
        {
            var servidorId = await SemearServidorAsync(ctx, RegimePrevidenciario.Rgps, 0);
            foreach (var t in TabelasFederaisSeed.Inss(TenantA))
            {
                ctx.TabelasInss.Add(t);
            }

            foreach (var t in TabelasFederaisSeed.Irrf(TenantA))
            {
                ctx.TabelasIrrf.Add(t);
            }

            // Rubrica cadastrada com vigencia que SO comeca em 2026-07 (posterior a competencia 2026-06):
            // logo ela NAO esta vigente na competencia apurada e nao aparece no catalogo de incidencias.
            ctx.Rubricas.Add(RubricaFolha.Criar(
                TenantA, Rubrica.De("VENCIMENTO"), "Vencimento", NaturezaRubrica.Provento,
                Competencia.De(2026, 7), incideInss: true, incideIrrf: true));

            var folha = FolhaDePagamento.Abrir(TenantA, competencia);
            folha.AdicionarEvento(servidorId, Rubrica.De("VENCIMENTO"), TipoEvento.Provento, BaseCalculo.De(5000m), 5000m, RegimePrevidenciario.Rgps);
            folhaId = folha.Id.Value;
            ctx.FolhasDePagamento.Add(folha);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CriarContexto(TenantA))
        {
            var handler = new ApurarDescontosLegaisHandler(
                new FolhaDePagamentoRepository(ctx), new RubricaFolhaRepository(ctx),
                new ServidorRegimeConsulta(ctx), new TabelasLegaisProvider(ctx),
                new ParametrosFolhaProviderFake(), ctx);

            // Sem o fail-closed, o motor presumiria (inss,rpps,irrf)=(false,false,false) -> base zero,
            // INSS/IRRF subtributados silenciosamente. Com o fix, a apuracao e interrompida.
            var acao = async () => await handler.Handle(new ApurarDescontosLegaisCommand(folhaId), default);
            (await acao.Should().ThrowAsync<CalculoFolhaException>())
                .Which.Message.Should().Contain("vigencia");

            // E nenhum desconto legal foi lancado (apuracao abortada antes de persistir).
            var folha = await ctx.FolhasDePagamento.Include(f => f.Eventos).SingleAsync(f => f.Id == new FolhaDePagamentoId(folhaId));
            folha.Eventos.Should().NotContain(e => e.Rubrica.Codigo == "INSS" || e.Rubrica.Codigo == "IRRF");
        }
    }
}
