using FluentAssertions;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Invariantes do motor contábil PCASP/MCASP:
/// 1) partida dobrada balanceada (ΣD = ΣC);
/// 2) homogeneidade de natureza de informação (sem cruzar naturezas);
/// 3) lançamento só em conta analítica (folha);
/// 4) período contábil aberto;
/// 5) estorno preserva trilha (gera lançamento inverso, não apaga).
/// </summary>
public sealed class ContabilidadeInvariantesTests
{
    private static readonly Guid TenantA = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static ContaContabil Conta(string codigo, TipoConta tipo = TipoConta.Analitica)
    {
        var c = CodigoContabil.De(codigo);
        var indicador = ContaContabil.ExigeIndicadorFP(c.Classe)
            ? IndicadorSuperavitFinanceiro.Financeiro
            : IndicadorSuperavitFinanceiro.NaoAplicavel;
        return ContaContabil.Criar(
            TenantA,
            c,
            $"Conta {codigo}",
            "Funcao.",
            "Funcionamento.",
            tipo,
            contaPaiId: null,
            indicador,
            encerramento: true);
    }

    private static LinhaLancamento Linha(ContaContabil conta, LadoPartida lado, decimal valor)
        => new(conta.Id, conta.Codigo, conta.NaturezaInformacao, conta.Tipo, lado, ValorMonetario.De(valor));

    // ---- Invariante 1: ΣD = ΣC ----

    [Fact]
    public void Lancamento_desbalanceado_e_rejeitado()
    {
        var disponivel = Conta("6.2.2.1.1.00.00");
        var empenhado = Conta("6.2.2.1.3.01.00");

        var acao = () => LancamentoContabil.Registrar(
            TenantA, new DateOnly(2026, 3, 10), 2026, "Teste", OrigemLancamento.Manual, null, null,
            [Linha(disponivel, LadoPartida.Debito, 100m), Linha(empenhado, LadoPartida.Credito, 90m)],
            periodoAberto: true);

        acao.Should().Throw<PartidaDobradaDesbalanceadaException>();
    }

    [Fact]
    public void Lancamento_balanceado_e_aceito_e_emite_evento()
    {
        var disponivel = Conta("6.2.2.1.1.00.00");
        var empenhado = Conta("6.2.2.1.3.01.00");

        var lancamento = LancamentoContabil.Registrar(
            TenantA, new DateOnly(2026, 3, 10), 2026, "Empenho", OrigemLancamento.EventoAutomatico, Guid.NewGuid(), Guid.NewGuid(),
            [Linha(disponivel, LadoPartida.Debito, 100m), Linha(empenhado, LadoPartida.Credito, 100m)],
            periodoAberto: true);

        lancamento.Partidas.Should().HaveCount(2);
        lancamento.PeriodoMes.Should().Be(3);
        lancamento.DomainEvents.Should().ContainSingle();
    }

    // ---- Invariante 2: mesma natureza ----

    [Fact]
    public void Lancamento_que_cruza_naturezas_e_rejeitado()
    {
        var orcamentaria = Conta("6.2.2.1.1.00.00"); // Orcamentaria
        var patrimonial = Conta("1.1.1.1.01"); // Patrimonial

        var acao = () => LancamentoContabil.Registrar(
            TenantA, new DateOnly(2026, 3, 10), 2026, "Misto", OrigemLancamento.Manual, null, null,
            [Linha(orcamentaria, LadoPartida.Debito, 100m), Linha(patrimonial, LadoPartida.Credito, 100m)],
            periodoAberto: true);

        acao.Should().Throw<NaturezasMisturadasException>();
    }

    // ---- Invariante 3: conta analítica (folha) ----

    [Fact]
    public void Lancamento_em_conta_sintetica_e_rejeitado()
    {
        var sintetica = Conta("6.2.2.1.1", TipoConta.Sintetica);
        var analitica = Conta("6.2.2.1.3.01.00");

        var acao = () => LancamentoContabil.Registrar(
            TenantA, new DateOnly(2026, 3, 10), 2026, "Teste", OrigemLancamento.Manual, null, null,
            [Linha(sintetica, LadoPartida.Debito, 100m), Linha(analitica, LadoPartida.Credito, 100m)],
            periodoAberto: true);

        acao.Should().Throw<ContaNaoAnaliticaException>();
    }

    // ---- Invariante 4: período aberto ----

    [Fact]
    public void Lancamento_em_periodo_fechado_e_rejeitado()
    {
        var d = Conta("6.2.2.1.1.00.00");
        var c = Conta("6.2.2.1.3.01.00");

        var acao = () => LancamentoContabil.Registrar(
            TenantA, new DateOnly(2026, 3, 10), 2026, "Teste", OrigemLancamento.Manual, null, null,
            [Linha(d, LadoPartida.Debito, 100m), Linha(c, LadoPartida.Credito, 100m)],
            periodoAberto: false);

        acao.Should().Throw<PeriodoContabilFechadoException>();
    }

    [Fact]
    public void Lancamento_exige_pelo_menos_um_debito_e_um_credito()
    {
        var a = Conta("6.2.2.1.1.00.00");
        var b = Conta("6.2.2.1.3.01.00");

        var acao = () => LancamentoContabil.Registrar(
            TenantA, new DateOnly(2026, 3, 10), 2026, "Teste", OrigemLancamento.Manual, null, null,
            [Linha(a, LadoPartida.Debito, 50m), Linha(b, LadoPartida.Debito, 50m)],
            periodoAberto: true);

        acao.Should().Throw<RoteiroContabilInvalidoException>();
    }

    // ---- Invariante 5: estorno preserva trilha ----

    [Fact]
    public void Estorno_inverte_lados_e_marca_original_sem_apagar()
    {
        var d = Conta("6.2.2.1.1.00.00");
        var c = Conta("6.2.2.1.3.01.00");
        var original = LancamentoContabil.Registrar(
            TenantA, new DateOnly(2026, 3, 10), 2026, "Empenho", OrigemLancamento.EventoAutomatico, null, null,
            [Linha(d, LadoPartida.Debito, 100m), Linha(c, LadoPartida.Credito, 100m)],
            periodoAberto: true);

        var estorno = original.Estornar(new DateOnly(2026, 3, 20), "Estorno do empenho", periodoAberto: true);

        original.Estornado.Should().BeTrue();
        original.LancamentoEstornoId.Should().Be(estorno.Id);
        estorno.Origem.Should().Be(OrigemLancamento.Estorno);
        estorno.Partidas.Single(p => p.ContaId == d.Id).Lado.Should().Be(LadoPartida.Credito);
        estorno.Partidas.Single(p => p.ContaId == c.Id).Lado.Should().Be(LadoPartida.Debito);
    }

    [Fact]
    public void Estorno_duplo_e_bloqueado()
    {
        var d = Conta("6.2.2.1.1.00.00");
        var c = Conta("6.2.2.1.3.01.00");
        var original = LancamentoContabil.Registrar(
            TenantA, new DateOnly(2026, 3, 10), 2026, "Empenho", OrigemLancamento.EventoAutomatico, null, null,
            [Linha(d, LadoPartida.Debito, 100m), Linha(c, LadoPartida.Credito, 100m)],
            periodoAberto: true);
        original.Estornar(new DateOnly(2026, 3, 20), "Estorno 1", periodoAberto: true);

        var acao = () => original.Estornar(new DateOnly(2026, 3, 21), "Estorno 2", periodoAberto: true);

        acao.Should().Throw<InvalidOperationException>();
    }

    // ---- CodigoContabil / ContaContabil ----

    [Fact]
    public void Codigo_deriva_classe_natureza_e_pai()
    {
        var codigo = CodigoContabil.De("6.2.2.1.3.01.00");

        codigo.Classe.Should().Be(6);
        codigo.Nivel.Should().Be(7);
        codigo.NaturezaInformacaoDefault().Should().Be(NaturezaInformacao.Orcamentaria);
        codigo.NaturezaSaldoDefault().Should().Be(NaturezaSaldo.Credora);
        codigo.CodigoPai().Should().Be("6.2.2.1.3.01");
    }

    [Theory]
    [InlineData("9.1")]          // classe invalida (>8)
    [InlineData("1.1.1.1.123")]  // segmento de detalhe com 3 digitos
    [InlineData("11.1")]         // nivel 1 com 2 digitos
    [InlineData("1..1")]         // segmento vazio
    public void Codigo_invalido_e_rejeitado(string codigo)
    {
        var acao = () => CodigoContabil.De(codigo);
        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Conta_ativo_sem_indicador_FP_e_rejeitada()
    {
        var acao = () => ContaContabil.Criar(
            TenantA, CodigoContabil.De("1.1.1.1.01"), "Caixa", "f", "f",
            TipoConta.Analitica, null, IndicadorSuperavitFinanceiro.NaoAplicavel, encerramento: false);

        acao.Should().Throw<RoteiroContabilInvalidoException>();
    }

    // ---- EventoContabil (roteiro) ----

    [Fact]
    public void Roteiro_orcamentario_balanceado_e_aceito_e_resolve_um_grupo()
    {
        var evento = EventoContabil.Criar(
            TenantA, FatoContabil.EmpenhoEmitido, "EVT-EMP", "Empenho", 0, null,
            [
                LinhaRoteiro.PorCodigo(LadoPartida.Debito, NaturezaInformacao.Orcamentaria, "6.2.2.1.1.00.00"),
                LinhaRoteiro.PorCodigo(LadoPartida.Credito, NaturezaInformacao.Orcamentaria, "6.2.2.1.3.01.00"),
            ]);

        var disponivel = Conta("6.2.2.1.1.00.00");
        var empenhado = Conta("6.2.2.1.3.01.00");
        var mapa = new Dictionary<string, ContaContabil>
        {
            ["6.2.2.1.1.00.00"] = disponivel,
            ["6.2.2.1.3.01.00"] = empenhado,
        };

        var grupos = evento.ResolverPara(ValorMonetario.De(250m), l => mapa[l.CodigoContaFixo!]);

        grupos.Should().HaveCount(1);
        grupos[0].Natureza.Should().Be(NaturezaInformacao.Orcamentaria);
        grupos[0].Linhas.Should().HaveCount(2);
    }

    [Fact]
    public void Roteiro_com_natureza_desbalanceada_e_rejeitado()
    {
        var acao = () => EventoContabil.Criar(
            TenantA, FatoContabil.EmpenhoEmitido, "EVT", "Bad", 0, null,
            [
                LinhaRoteiro.PorCodigo(LadoPartida.Debito, NaturezaInformacao.Orcamentaria, "6.2.2.1.1.00.00"),
            ]);

        acao.Should().Throw<RoteiroContabilInvalidoException>();
    }

    [Fact]
    public void Liquidacao_gera_dois_grupos_um_por_natureza()
    {
        var evento = EventoContabil.Criar(
            TenantA, FatoContabil.DespesaLiquidada, "EVT-LIQ", "Liquidacao", 0, null,
            [
                LinhaRoteiro.PorCodigo(LadoPartida.Debito, NaturezaInformacao.Orcamentaria, "6.2.2.1.3.01.00"),
                LinhaRoteiro.PorCodigo(LadoPartida.Credito, NaturezaInformacao.Orcamentaria, "6.2.2.1.3.03.00"),
                LinhaRoteiro.PorPapel(LadoPartida.Debito, NaturezaInformacao.Patrimonial, PapelConta.VpdServicos),
                LinhaRoteiro.PorPapel(LadoPartida.Credito, NaturezaInformacao.Patrimonial, PapelConta.FornecedoresCP),
            ]);

        var mapa = new Dictionary<string, ContaContabil>
        {
            ["6.2.2.1.3.01.00"] = Conta("6.2.2.1.3.01.00"),
            ["6.2.2.1.3.03.00"] = Conta("6.2.2.1.3.03.00"),
            ["VpdServicos"] = Conta("3.3.2.1.01"),
            ["FornecedoresCP"] = Conta("2.1.3.1.01"),
        };

        var grupos = evento.ResolverPara(ValorMonetario.De(500m), l =>
            l.CodigoContaFixo is not null ? mapa[l.CodigoContaFixo] : mapa[l.Papel!.ToString()!]);

        grupos.Should().HaveCount(2);
        grupos.Select(g => g.Natureza).Should().BeEquivalentTo(
            [NaturezaInformacao.Orcamentaria, NaturezaInformacao.Patrimonial]);

        // Cada grupo vira um lançamento homogêneo e balanceado.
        foreach (var grupo in grupos)
        {
            var lancamento = LancamentoContabil.Registrar(
                TenantA, new DateOnly(2026, 4, 1), 2026, "Liquidacao", OrigemLancamento.EventoAutomatico, Guid.NewGuid(), evento.Id.Value,
                grupo.Linhas, periodoAberto: true);
            lancamento.NaturezaInformacao.Should().Be(grupo.Natureza);
        }
    }
}
