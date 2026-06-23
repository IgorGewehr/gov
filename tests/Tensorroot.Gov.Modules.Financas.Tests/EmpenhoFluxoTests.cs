using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Prova do módulo Finanças sobre SQLite: ciclo Empenho → Liquidação → Pagamento (Lei 4.320),
/// gravação de auditoria e isolamento entre tenants.
/// </summary>
public sealed class EmpenhoFluxoTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly SqliteConnection _connection;

    public EmpenhoFluxoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task Ciclo_empenho_liquidacao_pagamento_persiste_e_respeita_isolamento()
    {
        var credor = Credor.PessoaJuridica("Fornecedor Exemplo LTDA", Cnpj.Create("11.222.333/0001-81"));

        await using (var contexto = CriarContexto(TenantA))
        {
            var empenho = Empenho.Emitir(
                TenantA,
                "2026NE000123",
                DotacaoOrcamentariaId.New(),
                credor,
                ValorMonetario.De(10000.00m),
                TipoEmpenho.Ordinario,
                2026,
                new DateOnly(2026, 6, 1));
            empenho.RegistrarLiquidacao(ValorMonetario.De(10000.00m));
            empenho.RegistrarPagamento(ValorMonetario.De(10000.00m));
            contexto.Empenhos.Add(empenho);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var empenho = await contexto.Empenhos.FirstAsync();
            empenho.Situacao.Should().Be(SituacaoEmpenho.TotalmentePago);
            empenho.SaldoALiquidar.Valor.Should().Be(0m);
            empenho.SaldoAPagar.Valor.Should().Be(0m);
            (await contexto.AuditTrail.ToListAsync()).Should().NotBeEmpty();
        }

        await using (var contexto = CriarContexto(Guid.NewGuid()))
        {
            (await contexto.Empenhos.ToListAsync()).Should().BeEmpty();
        }
    }

    [Fact]
    public void Pagamento_sem_liquidacao_previa_e_bloqueado()
    {
        var credor = Credor.PessoaJuridica("Fornecedor Exemplo LTDA", Cnpj.Create("11.222.333/0001-81"));
        var empenho = Empenho.Emitir(
            TenantA,
            "2026NE000999",
            DotacaoOrcamentariaId.New(),
            credor,
            ValorMonetario.De(500m),
            TipoEmpenho.Ordinario,
            2026,
            new DateOnly(2026, 6, 1));

        var acao = () => empenho.RegistrarPagamento(ValorMonetario.De(500m));

        acao.Should().Throw<Domain.Exceptions.SaldoLiquidacaoInsuficienteException>();
    }

    private static Empenho EmpenhoDe(decimal valor)
        => Empenho.Emitir(
            TenantA, "2026NE000777", DotacaoOrcamentariaId.New(),
            Credor.PessoaJuridica("Fornecedor Exemplo LTDA", Cnpj.Create("11.222.333/0001-81")),
            ValorMonetario.De(valor), TipoEmpenho.Ordinario, 2026, new DateOnly(2026, 6, 1));

    [Fact] // Liquidacao PARCIAL: empenho 10.000, liquida 4.000 -> ParcialmenteLiquidado, saldos coerentes.
    public void Liquidacao_parcial_mantem_saldos_coerentes()
    {
        var empenho = EmpenhoDe(10000m);

        empenho.RegistrarLiquidacao(ValorMonetario.De(4000m));

        empenho.Situacao.Should().Be(SituacaoEmpenho.ParcialmenteLiquidado);
        empenho.SaldoALiquidar.Valor.Should().Be(6000m);
        empenho.SaldoAPagar.Valor.Should().Be(4000m, "so o liquidado pode ser pago");
    }

    [Fact] // Estorno de liquidacao devolve saldo a liquidar (reversao do ato).
    public void Estorno_de_liquidacao_devolve_saldo_a_liquidar()
    {
        var empenho = EmpenhoDe(10000m);
        empenho.RegistrarLiquidacao(ValorMonetario.De(4000m));

        empenho.EstornarLiquidacao(ValorMonetario.De(1500m));

        empenho.ValorLiquidado.Valor.Should().Be(2500m);
        empenho.SaldoALiquidar.Valor.Should().Be(7500m);
    }

    [Fact] // C-T3: AnularTotal sobre empenho PARCIALMENTE liquidado falha com mensagem clara.
    public void Anular_total_de_empenho_parcialmente_liquidado_falha_com_mensagem_clara()
    {
        var empenho = EmpenhoDe(10000m);
        empenho.RegistrarLiquidacao(ValorMonetario.De(4000m));

        var acao = empenho.AnularTotal;

        acao.Should().Throw<InvalidOperationException>().WithMessage("*liquidacao*");
    }

    [Fact] // Anulacao PARCIAL reduz o saldo empenhado (reversao do empenho ainda nao liquidado).
    public void Anulacao_parcial_reduz_o_empenhado()
    {
        var empenho = EmpenhoDe(10000m);

        empenho.AnularParcial(ValorMonetario.De(3000m));

        empenho.ValorAnulado.Valor.Should().Be(3000m);
        empenho.SaldoEmpenhado.Valor.Should().Be(7000m);
        empenho.Situacao.Should().Be(SituacaoEmpenho.AnuladoParcial);
    }

    [Fact] // Restos a Pagar: empenho liquidado-nao-pago no fim do exercicio inscreve em RP (processado).
    public void Empenho_liquidado_nao_pago_inscreve_em_restos_a_pagar_processado()
    {
        var empenho = EmpenhoDe(10000m);
        empenho.RegistrarLiquidacao(ValorMonetario.De(10000m)); // processado: ha direito liquido e certo.

        empenho.PossuiSaldoParaRestosAPagar().Should().BeTrue();
        empenho.InscreverEmRestosAPagar(2026);

        empenho.Situacao.Should().Be(SituacaoEmpenho.InscritoRestosAPagar);
    }

    [Fact] // Restos a Pagar NAO processado: empenho empenhado-nao-liquidado tambem tem saldo a inscrever.
    public void Empenho_empenhado_nao_liquidado_distingue_rp_nao_processado()
    {
        var processado = EmpenhoDe(10000m);
        processado.RegistrarLiquidacao(ValorMonetario.De(10000m));

        var naoProcessado = EmpenhoDe(10000m); // sem liquidacao -> RP nao processado.

        // Ambos tem saldo a inscrever (empenhado - pago > 0); a distincao processado/nao-processado
        // se da pelo ValorLiquidado (EVT-RP-INSC a jusante usa essa informacao).
        processado.PossuiSaldoParaRestosAPagar().Should().BeTrue();
        naoProcessado.PossuiSaldoParaRestosAPagar().Should().BeTrue();
        processado.ValorLiquidado.Valor.Should().Be(10000m, "RP processado tem liquidacao");
        naoProcessado.ValorLiquidado.Valor.Should().Be(0m, "RP nao-processado nao tem liquidacao");
    }

    [Fact] // C-B6: pagar empenho ja INSCRITO em Restos a Pagar pelo fluxo ordinario e BLOQUEADO (fluxo proprio).
    public void Pagamento_ordinario_de_empenho_inscrito_em_restos_a_pagar_e_bloqueado()
    {
        var empenho = EmpenhoDe(10000m);
        empenho.RegistrarLiquidacao(ValorMonetario.De(10000m));
        empenho.InscreverEmRestosAPagar(2026);

        // Sem o fix C-B6, ValorPago subiria mas a situacao travaria em InscritoRestosAPagar (estado ambiguo).
        var acao = () => empenho.RegistrarPagamento(ValorMonetario.De(10000m));

        acao.Should().Throw<InvalidOperationException>().WithMessage("*nao admite*");
        empenho.ValorPago.Valor.Should().Be(0m, "o pagamento foi recusado antes de mexer no saldo");
        empenho.Situacao.Should().Be(SituacaoEmpenho.InscritoRestosAPagar);
    }

    [Fact] // C-B6: pagar empenho ja TOTALMENTE PAGO e bloqueado (idempotencia/estado terminal).
    public void Pagamento_de_empenho_totalmente_pago_e_bloqueado()
    {
        var empenho = EmpenhoDe(10000m);
        empenho.RegistrarLiquidacao(ValorMonetario.De(10000m));
        empenho.RegistrarPagamento(ValorMonetario.De(10000m));
        empenho.Situacao.Should().Be(SituacaoEmpenho.TotalmentePago);

        var acao = () => empenho.RegistrarPagamento(ValorMonetario.De(0.01m));

        acao.Should().Throw<InvalidOperationException>().WithMessage("*nao admite*");
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    private FinancasDbContext CriarContexto(Guid tenantId)
    {
        var tenant = new TenantContextFake(tenantId);
        var options = new DbContextOptionsBuilder<FinancasDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenant),
                new AuditSaveChangesInterceptor(new CurrentUserFake(), TimeProvider.System))
            .Options;

        var contexto = new FinancasDbContext(options, tenant);
        contexto.Database.EnsureCreated();
        return contexto;
    }
}

file sealed class TenantContextFake(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

file sealed class CurrentUserFake : ICurrentUser
{
    public string? UserId => "teste";

    public string? UserName => "Usuário de Teste";

    public string? IpAddress => "127.0.0.1";
}
