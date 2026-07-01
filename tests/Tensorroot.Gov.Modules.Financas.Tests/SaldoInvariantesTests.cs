using FluentAssertions;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Invariantes de saldo do ciclo da despesa pública (Lei 4.320/64):
/// 1) Não empenhar acima da dotação disponível (art. 60);
/// 2) Não liquidar acima do saldo do empenho (art. 63);
/// 3) Não pagar acima do saldo liquidado (art. 62).
/// Cada estágio nunca pode ultrapassar o estágio anterior.
/// </summary>
public sealed class SaldoInvariantesTests
{
    private static readonly Guid TenantA = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static ClassificacaoOrcamentaria Classificacao()
        => ClassificacaoOrcamentaria.De("02", "0201", "04.122.0002.2010", CategoriaEconomica.DespesasCorrentes, "0001");

    private static Credor CredorPadrao()
        => Credor.PessoaJuridica("Fornecedor Exemplo LTDA", Cnpj.Create("11.222.333/0001-81"));

    private static Empenho EmpenhoDe(decimal valor)
        => Empenho.Emitir(
            TenantA,
            "2026NE000001",
            DotacaoOrcamentariaId.New(),
            CredorPadrao(),
            ValorMonetario.De(valor),
            TipoEmpenho.Ordinario,
            2026,
            new DateOnly(2026, 6, 1));

    private static DocumentoComprobatorio Documento()
        => DocumentoComprobatorio.NotaFiscal("NF-001", new DateOnly(2026, 6, 1));

    // ---- Invariante 1: não empenhar acima da dotação ----

    [Fact]
    public void Nao_empenhar_acima_da_dotacao_disponivel()
    {
        var dotacao = DotacaoOrcamentaria.Criar(TenantA, 2026, Classificacao(), ValorMonetario.De(1000m));

        var acao = () => dotacao.ReservarEmpenho(ValorMonetario.De(1000.01m));

        acao.Should().Throw<SaldoOrcamentarioInsuficienteException>();
        dotacao.ValorEmpenhadoLiquido.Valor.Should().Be(0m);
        dotacao.SaldoDisponivel.Valor.Should().Be(1000m);
    }

    [Fact]
    public void Empenhar_exatamente_a_dotacao_zera_o_saldo_disponivel()
    {
        var dotacao = DotacaoOrcamentaria.Criar(TenantA, 2026, Classificacao(), ValorMonetario.De(1000m));

        dotacao.ReservarEmpenho(ValorMonetario.De(1000m));

        dotacao.SaldoDisponivel.Valor.Should().Be(0m);
    }

    [Fact] // Bug hunt: LiberarEmpenho acima do empenhado deve FALHAR, não truncar a zero (fail-closed).
    public void Liberar_mais_do_que_empenhado_e_rejeitado_em_vez_de_truncar()
    {
        var dotacao = DotacaoOrcamentaria.Criar(TenantA, 2026, Classificacao(), ValorMonetario.De(1000m));
        dotacao.ReservarEmpenho(ValorMonetario.De(400m));

        var acao = () => dotacao.LiberarEmpenho(ValorMonetario.De(500m));

        acao.Should().Throw<InvalidOperationException>();
        dotacao.ValorEmpenhadoLiquido.Valor.Should().Be(400m, "o saldo empenhado nao pode ser truncado a zero");
    }

    [Fact]
    public void Empenhos_sucessivos_nao_podem_estourar_a_dotacao()
    {
        var dotacao = DotacaoOrcamentaria.Criar(TenantA, 2026, Classificacao(), ValorMonetario.De(1000m));
        dotacao.ReservarEmpenho(ValorMonetario.De(600m));

        var acao = () => dotacao.ReservarEmpenho(ValorMonetario.De(401m));

        acao.Should().Throw<SaldoOrcamentarioInsuficienteException>();
        dotacao.SaldoDisponivel.Valor.Should().Be(400m);
    }

    [Fact]
    public void Dotacao_bloqueada_nao_aceita_empenho()
    {
        var dotacao = DotacaoOrcamentaria.Criar(TenantA, 2026, Classificacao(), ValorMonetario.De(1000m));
        dotacao.Bloquear();

        var acao = () => dotacao.ReservarEmpenho(ValorMonetario.De(100m));

        acao.Should().Throw<InvalidOperationException>();
    }

    // ---- Invariante 2: não liquidar acima do empenho ----

    [Fact]
    public void Nao_liquidar_acima_do_saldo_do_empenho()
    {
        var empenho = EmpenhoDe(1000m);

        var acao = () => empenho.RegistrarLiquidacao(ValorMonetario.De(1000.01m));

        acao.Should().Throw<SaldoEmpenhoInsuficienteException>();
        empenho.ValorLiquidado.Valor.Should().Be(0m);
        empenho.SaldoALiquidar.Valor.Should().Be(1000m);
    }

    [Fact]
    public void Liquidacoes_sucessivas_nao_podem_exceder_o_empenho()
    {
        var empenho = EmpenhoDe(1000m);
        empenho.RegistrarLiquidacao(ValorMonetario.De(700m));

        var acao = () => empenho.RegistrarLiquidacao(ValorMonetario.De(301m));

        acao.Should().Throw<SaldoEmpenhoInsuficienteException>();
        empenho.SaldoALiquidar.Valor.Should().Be(300m);
        empenho.Situacao.Should().Be(SituacaoEmpenho.ParcialmenteLiquidado);
    }

    [Fact]
    public void Liquidacao_apos_anulacao_respeita_o_saldo_remanescente()
    {
        var empenho = EmpenhoDe(1000m);
        empenho.AnularParcial(ValorMonetario.De(400m));

        // Saldo a liquidar agora é 600.
        empenho.RegistrarLiquidacao(ValorMonetario.De(600m));
        var acao = () => empenho.RegistrarLiquidacao(ValorMonetario.De(0.01m));

        acao.Should().Throw<SaldoEmpenhoInsuficienteException>();
        empenho.SaldoALiquidar.Valor.Should().Be(0m);
    }

    [Fact]
    public void Liquidacao_aggregate_nunca_paga_acima_do_proprio_valor()
    {
        var liquidacao = Liquidacao.Registrar(
            TenantA, EmpenhoId.New(), ValorMonetario.De(500m), new DateOnly(2026, 6, 1), Documento());

        var acao = () => liquidacao.RegistrarPagamento(ValorMonetario.De(500.01m));

        acao.Should().Throw<SaldoLiquidacaoInsuficienteException>();
    }

    // ---- Invariante 3: não pagar acima do liquidado ----

    [Fact]
    public void Nao_pagar_acima_do_saldo_liquidado()
    {
        var empenho = EmpenhoDe(1000m);
        empenho.RegistrarLiquidacao(ValorMonetario.De(400m));

        var acao = () => empenho.RegistrarPagamento(ValorMonetario.De(400.01m));

        acao.Should().Throw<SaldoLiquidacaoInsuficienteException>();
        empenho.ValorPago.Valor.Should().Be(0m);
        empenho.SaldoAPagar.Valor.Should().Be(400m);
    }

    [Fact]
    public void Pagar_sem_qualquer_liquidacao_e_bloqueado()
    {
        var empenho = EmpenhoDe(1000m);

        var acao = () => empenho.RegistrarPagamento(ValorMonetario.De(0.01m));

        acao.Should().Throw<SaldoLiquidacaoInsuficienteException>();
    }

    [Fact]
    public void Pagamentos_sucessivos_nao_podem_exceder_o_liquidado()
    {
        var empenho = EmpenhoDe(1000m);
        empenho.RegistrarLiquidacao(ValorMonetario.De(1000m));
        empenho.RegistrarPagamento(ValorMonetario.De(600m));

        var acao = () => empenho.RegistrarPagamento(ValorMonetario.De(401m));

        acao.Should().Throw<SaldoLiquidacaoInsuficienteException>();
        empenho.SaldoAPagar.Valor.Should().Be(400m);
        empenho.Situacao.Should().Be(SituacaoEmpenho.ParcialmentePago);
    }

    // ---- Cadeia completa respeitando a ordem dos estágios ----

    [Fact]
    public void Ciclo_completo_respeita_a_cadeia_dotacao_empenho_liquidacao_pagamento()
    {
        var dotacao = DotacaoOrcamentaria.Criar(TenantA, 2026, Classificacao(), ValorMonetario.De(1000m));
        dotacao.ReservarEmpenho(ValorMonetario.De(1000m));

        var empenho = EmpenhoDe(1000m);
        empenho.RegistrarLiquidacao(ValorMonetario.De(1000m));
        empenho.RegistrarPagamento(ValorMonetario.De(1000m));

        dotacao.SaldoDisponivel.Valor.Should().Be(0m);
        empenho.Situacao.Should().Be(SituacaoEmpenho.TotalmentePago);
        empenho.SaldoALiquidar.Valor.Should().Be(0m);
        empenho.SaldoAPagar.Valor.Should().Be(0m);
    }

    [Fact]
    public void Estagios_intermediarios_jamais_ultrapassam_o_estagio_anterior()
    {
        var empenho = EmpenhoDe(1000m);
        empenho.RegistrarLiquidacao(ValorMonetario.De(800m));
        empenho.RegistrarPagamento(ValorMonetario.De(800m));

        // Liquidado <= Empenhado e Pago <= Liquidado, sempre.
        empenho.ValorLiquidado.Valor.Should().BeLessThanOrEqualTo(empenho.SaldoEmpenhado.Valor);
        empenho.ValorPago.Valor.Should().BeLessThanOrEqualTo(empenho.ValorLiquidado.Valor);
    }
}
