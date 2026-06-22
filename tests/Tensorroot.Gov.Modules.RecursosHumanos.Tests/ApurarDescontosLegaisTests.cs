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
            servidor.AdicionarDependente($"Dep {i}", "Filho", new DateOnly(2015, 1, 1));
        }

        ctx.Servidores.Add(servidor);
        await ctx.SaveChangesAsync();
        return servidor.Id.Value;
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
}
