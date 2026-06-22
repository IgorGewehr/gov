using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Msc;
using Tensorroot.Gov.Modules.Financas.Contracts;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Motor;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Commands;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.ReadModels;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.SharedKernel;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Geração da MSC ponta a ponta (SQLite): após contabilizar o ciclo da despesa e projetar o balancete,
/// o <see cref="GerarMscCommand"/> deriva a MSC, enfileira o <see cref="MSCGeradaIntegrationEvent"/> no
/// Outbox e grava o registro de idempotência. Reexecutar não republica (idempotência por competência).
/// </summary>
public sealed class GerarMscHandlerTests
{
    private static readonly Guid Tenant = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Fact]
    public async Task Gera_msc_publica_evento_no_outbox_e_e_idempotente()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();

        // Cada bloco using representa um ESCOPO próprio (como em runtime, onde o holder é scoped):
        // um holder NOVO por contexto. O ScopeDbContextHolder agora falha-alto se dois contextos
        // divergentes compartilharem o mesmo holder (H5).
        await using (var ctx = NovoContexto(conexao, new ScopeDbContextHolder()))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        await SemearAsync(conexao);
        await ContabilizarCicloAsync(conexao);
        await ProjetarBalanceteAsync(conexao);

        // Gera a MSC de 2026-05 (mês do ciclo contabilizado).
        var holderGeracao = new ScopeDbContextHolder();
        await using (var ctx = NovoContexto(conexao, holderGeracao))
        {
            var resultado = await Handler(ctx, holderGeracao).Handle(new GerarMscCommand(2026, 5, "10001"), CancellationToken.None);
            resultado.JaExistia.Should().BeFalse();
            resultado.QuantidadeLinhas.Should().BeGreaterThan(0);
        }

        // O evento foi enfileirado no Outbox (pendente) com o tipo correto e tenant carimbado.
        await using (var leitura = NovoContexto(conexao, new ScopeDbContextHolder()))
        {
            var mensagens = await leitura.Set<OutboxMessage>().ToListAsync();
            var mscs = mensagens.Where(m => m.Type.Contains(nameof(MSCGeradaIntegrationEvent), StringComparison.Ordinal)).ToList();
            mscs.Should().ContainSingle();
            mscs[0].TenantId.Should().Be(Tenant);
        }

        // Idempotência: reexecutar não cria novo evento nem novo registro.
        var holderRepeticao = new ScopeDbContextHolder();
        await using (var ctx = NovoContexto(conexao, holderRepeticao))
        {
            var repetido = await Handler(ctx, holderRepeticao).Handle(new GerarMscCommand(2026, 5, "10001"), CancellationToken.None);
            repetido.JaExistia.Should().BeTrue();
        }

        await using (var leitura = NovoContexto(conexao, new ScopeDbContextHolder()))
        {
            (await leitura.MscsGeradas.CountAsync()).Should().Be(1);
            var mensagens = await leitura.Set<OutboxMessage>().ToListAsync();
            var msc = mensagens.Count(m => m.Type.Contains(nameof(MSCGeradaIntegrationEvent), StringComparison.Ordinal));
            msc.Should().Be(1);
        }
    }

    [Fact]
    public async Task Msc_publicada_fecha_saldo_final_devedor_igual_credor()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();

        await using (var ctx = NovoContexto(conexao, new ScopeDbContextHolder()))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        await SemearAsync(conexao);
        await ContabilizarCicloAsync(conexao);
        await ProjetarBalanceteAsync(conexao);

        var holderGeracao = new ScopeDbContextHolder();
        await using (var ctx = NovoContexto(conexao, holderGeracao))
        {
            await Handler(ctx, holderGeracao).Handle(new GerarMscCommand(2026, 5, null), CancellationToken.None);
        }

        await using (var leitura = NovoContexto(conexao, new ScopeDbContextHolder()))
        {
            var mensagens = await leitura.Set<OutboxMessage>().ToListAsync();
            var mensagem = mensagens.First(m => m.Type.Contains(nameof(MSCGeradaIntegrationEvent), StringComparison.Ordinal));
            var evento = System.Text.Json.JsonSerializer.Deserialize<MSCGeradaIntegrationEvent>(mensagem.Content)!;

            const int saldoFinal = 3;
            const int devedor = (int)NaturezaSaldo.Devedora;
            const int credor = (int)NaturezaSaldo.Credora;
            var totalD = evento.Linhas.Where(l => l.TipoValor == saldoFinal && l.NaturezaSaldo == devedor).Sum(l => l.Valor);
            var totalC = evento.Linhas.Where(l => l.TipoValor == saldoFinal && l.NaturezaSaldo == credor).Sum(l => l.Valor);
            totalD.Should().Be(totalC);
        }
    }

    private static GerarMscHandler Handler(FinancasDbContext ctx, ScopeDbContextHolder holder)
        => new(
            new BalanceteProjection(ctx),
            new MscGeradaStore(ctx),
            new ModuleIntegrationEventWriter(holder, TimeProvider.System),
            new UnitOfWorkFake(ctx),
            new TenantFake(),
            TimeProvider.System);

    private static FinancasDbContext NovoContexto(SqliteConnection conexao, ScopeDbContextHolder holder)
    {
        var options = new DbContextOptionsBuilder<FinancasDbContext>().UseSqlite(conexao).Options;
        return new FinancasDbContext(options, new TenantFake(), holder);
    }

    private static async Task SemearAsync(SqliteConnection conexao)
    {
        var holder = new ScopeDbContextHolder();
        await using var ctx = NovoContexto(conexao, holder);
        var handler = new SemearPlanoDeContasHandler(
            new ContaContabilRepository(ctx),
            new EventoContabilRepository(ctx),
            ctx,
            new TenantFake());
        await handler.Handle(new SemearPlanoDeContasCommand(), CancellationToken.None);
    }

    private static async Task ContabilizarCicloAsync(SqliteConnection conexao)
    {
        var holder = new ScopeDbContextHolder();
        await using var ctx = NovoContexto(conexao, holder);
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

    private static async Task ProjetarBalanceteAsync(SqliteConnection conexao)
    {
        var holder = new ScopeDbContextHolder();
        await using var ctx = NovoContexto(conexao, holder);
        var handler = new Application.Contabilidade.Handlers.ProjetarBalanceteHandler(
            new LancamentoContabilRepository(ctx),
            new ContaContabilRepository(ctx),
            new BalanceteProjection(ctx),
            ctx);
        var ids = await ctx.LancamentosContabeis.Select(l => new { l.Id, l.Exercicio, l.PeriodoMes }).ToListAsync();
        foreach (var l in ids)
        {
            await handler.Handle(
                new Domain.Contabilidade.Events.LancamentoContabilRegistrado(l.Id, l.Exercicio, l.PeriodoMes),
                CancellationToken.None);
        }
    }

    private sealed class TenantFake : ITenantContext
    {
        public Guid TenantId => Tenant;

        public bool HasTenant => true;
    }

    private sealed class UnitOfWorkFake(FinancasDbContext context) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => context.SaveChangesAsync(cancellationToken);
    }
}
