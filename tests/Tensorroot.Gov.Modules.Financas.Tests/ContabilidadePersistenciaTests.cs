using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Commands;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Handlers;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Motor;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Events;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.ReadModels;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Persistência e projeção contábil ponta a ponta (SQLite em memória): valida mapeamentos EF
/// (owned collections, value converters do PCASP), o seed do plano/roteiros, a contabilização
/// automática de um fato e o balancete — incluindo a conferência D=C global por natureza.
/// </summary>
public sealed class ContabilidadePersistenciaTests
{
    private static readonly Guid Tenant = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static FinancasDbContext NovoContexto(SqliteConnection conexao)
    {
        var options = new DbContextOptionsBuilder<FinancasDbContext>().UseSqlite(conexao).Options;
        return new FinancasDbContext(options, new TenantFake());
    }

    [Fact]
    public async Task Seed_persiste_plano_e_roteiros_com_owned_collections()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        await using (var ctx = NovoContexto(conexao))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        await SemearAsync(conexao);

        await using var leitura = NovoContexto(conexao);
        (await leitura.ContasContabeis.CountAsync()).Should().BeGreaterThan(10);
        var roteiroLiq = await leitura.EventosContabeis
            .Include(e => e.Linhas)
            .FirstAsync(e => e.Fato == FatoContabil.DespesaLiquidada);
        roteiroLiq.Linhas.Should().HaveCount(4); // 2 orcamentarias + 2 patrimoniais
    }

    [Fact]
    public async Task Contabilizar_liquidacao_gera_dois_lancamentos_e_balancete_fecha_DC()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        await using (var ctx = NovoContexto(conexao))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        await SemearAsync(conexao);

        var origemId = Guid.NewGuid();
        await using (var ctx = NovoContexto(conexao))
        {
            var motor = new MotorContabil(
                new EventoContabilRepository(ctx),
                new ContaContabilRepository(ctx),
                new LancamentoContabilRepository(ctx),
                new TenantFake());

            var gerados = await motor.ContabilizarAsync(
                FatoContabil.DespesaLiquidada,
                ValorMonetario.De(1000m),
                new DateOnly(2026, 5, 10),
                origemId,
                CancellationToken.None);

            gerados.Should().Be(2); // orcamentario + patrimonial
            await ctx.SaveChangesAsync();
        }

        // Idempotência: repetir não duplica.
        await using (var ctx = NovoContexto(conexao))
        {
            var motor = new MotorContabil(
                new EventoContabilRepository(ctx),
                new ContaContabilRepository(ctx),
                new LancamentoContabilRepository(ctx),
                new TenantFake());
            var repetido = await motor.ContabilizarAsync(
                FatoContabil.DespesaLiquidada, ValorMonetario.De(1000m),
                new DateOnly(2026, 5, 10), origemId, CancellationToken.None);
            repetido.Should().Be(0);
        }

        // Projeta o balancete a partir dos lançamentos persistidos.
        await using (var ctx = NovoContexto(conexao))
        {
            var projection = new BalanceteProjection(ctx);
            var lancamentos = await ctx.LancamentosContabeis.Include(l => l.Partidas).ToListAsync();
            lancamentos.Should().HaveCount(2);

            foreach (var lancamento in lancamentos)
            {
                foreach (var partida in lancamento.Partidas)
                {
                    var conta = await ctx.ContasContabeis.FirstAsync(c => c.Id == partida.ContaId);
                    var linha = await projection.ObterOuCriarLinhaAsync(conta, lancamento.Exercicio, lancamento.PeriodoMes, CancellationToken.None);
                    if (partida.Lado == LadoPartida.Debito)
                    {
                        linha.TotalDebitos += partida.Valor.Valor;
                    }
                    else
                    {
                        linha.TotalCreditos += partida.Valor.Valor;
                    }

                    linha.RecalcularSaldo();
                }
            }

            await ctx.SaveChangesAsync();
        }

        // Conferência D=C global do período.
        await using (var leitura = NovoContexto(conexao))
        {
            var linhas = await leitura.BalancetesConta.Where(l => l.Exercicio == 2026 && l.PeriodoMes == 5).ToListAsync();
            linhas.Sum(l => l.TotalDebitos).Should().Be(linhas.Sum(l => l.TotalCreditos));
            linhas.Sum(l => l.TotalDebitos).Should().Be(2000m); // 1000 orcamentario + 1000 patrimonial
        }
    }

    [Fact]
    public async Task Projetar_multiplos_lancamentos_que_compartilham_conta_popula_balancete_e_fecha_DC()
    {
        // Reproduz o fluxo do Outbox: empenho, liquidacao e pagamento geram lancamentos que
        // COMPARTILHAM contas (6.2.2.1.3.01 e .03). O ProjetarBalanceteHandler roda para cada
        // LancamentoContabilRegistrado no MESMO contexto (como o OutboxPublisher drena o lote).
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        await using (var ctx = NovoContexto(conexao))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        await SemearAsync(conexao);

        // Gera os lancamentos dos tres fatos do ciclo (todos no exercicio/mes 2026-05).
        await using (var ctx = NovoContexto(conexao))
        {
            var motor = new MotorContabil(
                new EventoContabilRepository(ctx),
                new ContaContabilRepository(ctx),
                new LancamentoContabilRepository(ctx),
                new TenantFake());
            var data = new DateOnly(2026, 5, 10);
            await motor.ContabilizarAsync(FatoContabil.EmpenhoEmitido, ValorMonetario.De(1000m), data, Guid.NewGuid(), CancellationToken.None);
            await motor.ContabilizarAsync(FatoContabil.DespesaLiquidada, ValorMonetario.De(1000m), data, Guid.NewGuid(), CancellationToken.None);
            await motor.ContabilizarAsync(FatoContabil.PagamentoEfetuado, ValorMonetario.De(1000m), data, Guid.NewGuid(), CancellationToken.None);
            await ctx.SaveChangesAsync();
        }

        // Projeta TODOS os lancamentos num unico contexto (como o lote do Outbox).
        await using (var ctx = NovoContexto(conexao))
        {
            var handler = new ProjetarBalanceteHandler(
                new LancamentoContabilRepository(ctx),
                new ContaContabilRepository(ctx),
                new BalanceteProjection(ctx),
                ctx);

            var ids = await ctx.LancamentosContabeis.Select(l => new { l.Id, l.Exercicio, l.PeriodoMes }).ToListAsync();
            ids.Should().HaveCount(5); // empenho(1) + liquidacao(2) + pagamento(2)

            foreach (var l in ids)
            {
                await handler.Handle(new LancamentoContabilRegistrado(l.Id, l.Exercicio, l.PeriodoMes), CancellationToken.None);
            }
        }

        // Conferencia: balancete populado e D=C global.
        await using (var leitura = NovoContexto(conexao))
        {
            var linhas = await leitura.BalancetesConta.Where(l => l.Exercicio == 2026 && l.PeriodoMes == 5).ToListAsync();
            linhas.Should().NotBeEmpty();
            linhas.Sum(l => l.TotalDebitos).Should().Be(linhas.Sum(l => l.TotalCreditos));
            // empenho 1000 + liquidacao 2000 (orc+pat) + pagamento 2000 (orc+pat) = 5000 de cada lado.
            linhas.Sum(l => l.TotalDebitos).Should().Be(5000m);
        }
    }

    private static async Task SemearAsync(SqliteConnection conexao)
    {
        await using var ctx = NovoContexto(conexao);
        var handler = new SemearPlanoDeContasHandler(
            new ContaContabilRepository(ctx),
            new EventoContabilRepository(ctx),
            ctx,
            new TenantFake());
        await handler.Handle(new SemearPlanoDeContasCommand(), CancellationToken.None);
    }

    private sealed class TenantFake : ITenantContext
    {
        public Guid TenantId => Tenant;

        public bool HasTenant => true;
    }
}
