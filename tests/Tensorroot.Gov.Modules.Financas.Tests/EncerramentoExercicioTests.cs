using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Commands;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Encerramento;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Handlers;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Encerramento;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.ReadModels;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Encerramento de exercício ponta a ponta (SQLite em memória): apuração patrimonial (zera 3/4 contra
/// 2.3.7.1.1.01.00), balancete de encerramento ΣD=ΣC, transposição/abertura do resultado e
/// idempotência (reexecutar uma fase não duplica lançamentos). DESIGN encerramento §§4-5/§11.
/// </summary>
public sealed class EncerramentoExercicioTests
{
    private const int Exercicio = 2026;
    private static readonly Guid Tenant = Guid.Parse("77777777-7777-7777-7777-777777777777");

    // Lançamentos já projetados no balancete (idempotência da projeção manual no teste).
    private readonly HashSet<Guid> _projetados = [];

    private static FinancasDbContext NovoContexto(SqliteConnection conexao)
    {
        var options = new DbContextOptionsBuilder<FinancasDbContext>().UseSqlite(conexao).Options;
        return new FinancasDbContext(options, new TenantFake());
    }

    [Fact]
    public async Task Apuracao_patrimonial_com_VPA_maior_que_VPD_gera_superavit_e_zera_classes_3_e_4()
    {
        using var conexao = await PrepararBaseComMovimentoAsync(vpa: 1500m, vpd: 1000m);

        await ApurarPatrimonialAsync(conexao);

        await using var leitura = NovoContexto(conexao);

        // 2.3.7.1.1.01.00 credora = superavit de 500 (1500 VPA - 1000 VPD).
        var resultado = await SaldoMes13Async(leitura, MotorEncerramento.ContaResultadoExercicio);
        resultado.Should().Be(500m); // credor positivo

        // Classes 3 e 4 zeradas no mes 13.
        (await SaldoMes13Async(leitura, "3.3.2.1.01")).Should().Be(0m);
        (await SaldoMes13Async(leitura, "4.1.1.1.01")).Should().Be(0m);

        // Balancete de encerramento (mes 13) fecha D=C.
        var linhas13 = await leitura.BalancetesConta.Where(l => l.Exercicio == Exercicio && l.PeriodoMes == 13).ToListAsync();
        linhas13.Sum(l => l.TotalDebitos).Should().Be(linhas13.Sum(l => l.TotalCreditos));
    }

    [Fact]
    public async Task Apuracao_patrimonial_com_VPD_maior_que_VPA_gera_deficit_devedor()
    {
        using var conexao = await PrepararBaseComMovimentoAsync(vpa: 1000m, vpd: 1700m);

        await ApurarPatrimonialAsync(conexao);

        await using var leitura = NovoContexto(conexao);
        // Conta credora com saldo NEGATIVO = deficit (saldo devedor de 700).
        (await SaldoMes13Async(leitura, MotorEncerramento.ContaResultadoExercicio)).Should().Be(-700m);
    }

    [Fact]
    public async Task Apuracao_patrimonial_eh_idempotente_nao_duplica_lancamentos()
    {
        using var conexao = await PrepararBaseComMovimentoAsync(vpa: 1500m, vpd: 1000m);

        await ApurarPatrimonialAsync(conexao);
        var aposPrimeira = await ContarLancamentosEncerramentoAsync(conexao);

        // Reexecuta a fase: motor le ExisteParaOrigemAsync e nao gera novos lancamentos.
        await using (var ctx = NovoContexto(conexao))
        {
            var motor = NovoMotor(ctx);
            var gerados = await motor.ApurarResultadoPatrimonialAsync(Exercicio, CancellationToken.None);
            await ctx.SaveChangesAsync();
            gerados.Should().Be(0);
        }

        (await ContarLancamentosEncerramentoAsync(conexao)).Should().Be(aposPrimeira);
    }

    [Fact]
    public async Task Abertura_transfere_resultado_para_exercicios_anteriores_e_fecha_DC()
    {
        using var conexao = await PrepararBaseComMovimentoAsync(vpa: 1500m, vpd: 1000m);
        await ApurarPatrimonialAsync(conexao);

        // Abertura do exercicio seguinte (mes 0 de 2027): D ...01 / C ...02 pelo superavit.
        await using (var ctx = NovoContexto(conexao))
        {
            var motor = NovoMotor(ctx);
            var gerados = await motor.AbrirExercicioSeguinteAsync(Exercicio, CancellationToken.None);
            await ctx.SaveChangesAsync();
            gerados.Should().Be(1);
        }

        await ProjetarPendentesAsync(conexao);

        await using var leitura = NovoContexto(conexao);
        var anteriores = leitura.ContasContabeis.First(c => c.Codigo == CodigoContabil.De(MotorEncerramento.ContaResultadoAnteriores));
        var linhaAnteriores = await leitura.BalancetesConta
            .FirstAsync(l => l.ContaId == anteriores.Id.Value && l.Exercicio == 2027 && l.PeriodoMes == 0);
        linhaAnteriores.SaldoAtual.Should().Be(500m); // resultado migrou para ...02

        // BP de abertura fecha D=C.
        var abertura = await leitura.BalancetesConta.Where(l => l.Exercicio == 2027 && l.PeriodoMes == 0).ToListAsync();
        abertura.Sum(l => l.TotalDebitos).Should().Be(abertura.Sum(l => l.TotalCreditos));
    }

    [Fact]
    public void Agregado_congela_apos_encerrado_e_recusa_pular_fases()
    {
        var encerramento = EncerramentoExercicio.Iniciar(Tenant, Exercicio, DateTime.UtcNow);
        encerramento.Status.Should().Be(StatusEncerramento.Aberto);
        encerramento.Congelado.Should().BeFalse();

        // Avanca fase a fase ate Encerrado.
        encerramento.TentarAvancarPara(StatusEncerramento.RapInscrito).Should().BeTrue();
        encerramento.TentarAvancarPara(StatusEncerramento.EncerramentoParcial).Should().BeTrue();
        encerramento.TentarAvancarPara(StatusEncerramento.ApuracaoPatrimonial).Should().BeTrue();
        encerramento.TentarAvancarPara(StatusEncerramento.ApuracaoOrcamentaria).Should().BeTrue();
        encerramento.TentarAvancarPara(StatusEncerramento.Encerrado).Should().BeTrue();
        encerramento.Congelado.Should().BeTrue();

        // Idempotencia: reavancar a uma fase ja concluida e no-op (false), nao lanca.
        encerramento.TentarAvancarPara(StatusEncerramento.ApuracaoPatrimonial).Should().BeFalse();
    }

    [Fact]
    public void Agregado_recusa_transicao_que_pula_fases()
    {
        var encerramento = EncerramentoExercicio.Iniciar(Tenant, Exercicio, DateTime.UtcNow);
        var pular = () => encerramento.TentarAvancarPara(StatusEncerramento.Encerrado);
        pular.Should().Throw<InvalidOperationException>();
    }

    // ---- helpers ----

    private async Task<SqliteConnection> PrepararBaseComMovimentoAsync(decimal vpa, decimal vpd)
    {
        var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        await using (var ctx = NovoContexto(conexao))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        // Seed do plano (inclui as contas de encerramento 2.3.7.1.1.01/02/03).
        await using (var ctx = NovoContexto(conexao))
        {
            var handler = new SemearPlanoDeContasHandler(
                new ContaContabilRepository(ctx), new EventoContabilRepository(ctx), ctx, new TenantFake());
            await handler.Handle(new SemearPlanoDeContasCommand(), CancellationToken.None);
        }

        // Movimento patrimonial de dezembro: reconhece VPA (4) e VPD (3) por partida dobrada
        // patrimonial (a contrapartida vai a Ativo/Passivo permanentes, que transferem saldo).
        await using (var ctx = NovoContexto(conexao))
        {
            var contas = new ContaContabilRepository(ctx);
            var lancamentos = new LancamentoContabilRepository(ctx);

            // VPA: D Caixa (1) / C VPA (4)
            await RegistrarAsync(ctx, contas, lancamentos, "1.1.1.1.01", "4.1.1.1.01", vpa, "Reconhece VPA");
            // VPD: D VPD (3) / C Fornecedores (2)
            await RegistrarAsync(ctx, contas, lancamentos, "3.3.2.1.01", "2.1.3.1.01", vpd, "Reconhece VPD");
            await ctx.SaveChangesAsync();
        }

        await ProjetarPendentesAsync(conexao);
        return conexao;
    }

    private static async Task RegistrarAsync(
        FinancasDbContext ctx,
        ContaContabilRepository contas,
        LancamentoContabilRepository lancamentos,
        string codigoDebito,
        string codigoCredito,
        decimal valor,
        string historico)
    {
        var debito = await contas.ObterPorCodigoAsync(codigoDebito, CancellationToken.None);
        var credito = await contas.ObterPorCodigoAsync(codigoCredito, CancellationToken.None);
        var v = ValorMonetario.De(valor);

        var lancamento = LancamentoContabil.Registrar(
            Tenant,
            new DateOnly(Exercicio, 12, 20),
            Exercicio,
            historico,
            OrigemLancamento.Manual,
            Guid.NewGuid(),
            eventoContabilId: null,
            new[]
            {
                new LinhaLancamento(debito!.Id, debito.Codigo, debito.NaturezaInformacao, debito.Tipo, LadoPartida.Debito, v),
                new LinhaLancamento(credito!.Id, credito.Codigo, credito.NaturezaInformacao, credito.Tipo, LadoPartida.Credito, v),
            },
            periodoAberto: true);
        lancamentos.Adicionar(lancamento);
    }

    private async Task ApurarPatrimonialAsync(SqliteConnection conexao)
    {
        await using (var ctx = NovoContexto(conexao))
        {
            var motor = NovoMotor(ctx);
            await motor.ApurarResultadoPatrimonialAsync(Exercicio, CancellationToken.None);
            await ctx.SaveChangesAsync();
        }

        await ProjetarPendentesAsync(conexao);
    }

    private static MotorEncerramento NovoMotor(FinancasDbContext ctx)
        => new(
            new ContaContabilRepository(ctx),
            new LancamentoContabilRepository(ctx),
            new BalanceteProjection(ctx),
            new TenantFake());

    /// <summary>
    /// Projeta no balancete os lançamentos ainda NÃO projetados (idempotência via <see cref="_projetados"/>),
    /// na ordem de período (mês 0 e 13 incluídos) para que o saldo anterior derive corretamente.
    /// </summary>
    private async Task ProjetarPendentesAsync(SqliteConnection conexao)
    {
        await using var ctx = NovoContexto(conexao);
        var projection = new BalanceteProjection(ctx);
        var lancamentos = await ctx.LancamentosContabeis
            .Include(l => l.Partidas)
            .OrderBy(l => l.Exercicio).ThenBy(l => l.PeriodoMes)
            .ToListAsync();

        foreach (var lancamento in lancamentos)
        {
            if (!_projetados.Add(lancamento.Id.Value))
            {
                continue; // já projetado
            }

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

    private static async Task<decimal> SaldoMes13Async(FinancasDbContext ctx, string codigo)
    {
        var conta = ctx.ContasContabeis.First(c => c.Codigo == CodigoContabil.De(codigo));
        var linha = await ctx.BalancetesConta
            .FirstOrDefaultAsync(l => l.ContaId == conta.Id.Value && l.Exercicio == Exercicio && l.PeriodoMes == 13);
        return linha?.SaldoAtual ?? 0m;
    }

    private static async Task<int> ContarLancamentosEncerramentoAsync(SqliteConnection conexao)
    {
        await using var ctx = NovoContexto(conexao);
        return await ctx.LancamentosContabeis.CountAsync(l => l.Origem == OrigemLancamento.Encerramento);
    }

    private sealed class TenantFake : ITenantContext
    {
        public Guid TenantId => Tenant;

        public bool HasTenant => true;
    }
}
