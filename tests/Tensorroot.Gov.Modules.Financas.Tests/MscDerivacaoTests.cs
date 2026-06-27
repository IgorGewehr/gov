using FluentAssertions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Msc;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Msc;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// MSC derivada do balancete: cada conta gera registros SaldoInicial/Movimento/SaldoFinal; a soma da MSC
/// bate com o balancete (Σ confere) e a matriz só fecha quando D = C.
/// </summary>
public sealed class MscDerivacaoTests
{
    private static readonly Guid Tenant = Guid.Parse("77777777-7777-7777-7777-777777777777");

    // Balancete mínimo balanceado: uma conta devedora (ativo) e uma credora (passivo) com o mesmo
    // saldo final — espelha a identidade da partida dobrada.
    private static IReadOnlyList<LinhaBalancete> BalanceteBalanceado() =>
    [
        Linha("1.1.1.1.01", NaturezaSaldo.Devedora, saldoAnterior: 100m, debitos: 500m, creditos: 300m, saldoAtual: 300m),
        Linha("2.1.3.1.01", NaturezaSaldo.Credora, saldoAnterior: 100m, debitos: 300m, creditos: 500m, saldoAtual: 300m),
    ];

    private static LinhaBalancete Linha(
        string codigo,
        NaturezaSaldo natureza,
        decimal saldoAnterior,
        decimal debitos,
        decimal creditos,
        decimal saldoAtual)
        => new()
        {
            TenantId = Tenant,
            ContaId = Guid.NewGuid(),
            CodigoConta = codigo,
            Titulo = $"Conta {codigo}",
            NaturezaSaldo = natureza,
            NaturezaInformacao = NaturezaInformacao.Patrimonial,
            Nivel = 5,
            Exercicio = 2026,
            PeriodoMes = 3,
            SaldoAnterior = saldoAnterior,
            TotalDebitos = debitos,
            TotalCreditos = creditos,
            SaldoAtual = saldoAtual,
        };

    [Fact]
    public void Deriva_ate_quatro_registros_por_conta_com_valores_nao_negativos()
    {
        var balancete = BalanceteBalanceado();

        var linhas = DerivadorMsc.Derivar(balancete, poderOrgao: "10001");

        // Cada conta: SaldoInicial(1) + Movimento D + Movimento C + SaldoFinal = 4 registros.
        linhas.Should().HaveCount(8);
        linhas.Should().OnlyContain(l => l.Valor >= 0m);
        linhas.Should().OnlyContain(l => l.Complementares.PoderOrgao == "10001");
    }

    [Fact]
    public void Movimento_do_periodo_bate_com_totais_de_debito_e_credito_do_balancete()
    {
        var balancete = BalanceteBalanceado();

        var linhas = DerivadorMsc.Derivar(balancete, poderOrgao: null);

        var movimentoDebito = linhas
            .Where(l => l.TipoValor == TipoValorMsc.MovimentoPeriodo && l.NaturezaSaldo == NaturezaSaldo.Devedora)
            .Sum(l => l.Valor);
        var movimentoCredito = linhas
            .Where(l => l.TipoValor == TipoValorMsc.MovimentoPeriodo && l.NaturezaSaldo == NaturezaSaldo.Credora)
            .Sum(l => l.Valor);

        // Σ débitos do balancete = 500 + 300; Σ créditos = 300 + 500.
        movimentoDebito.Should().Be(balancete.Sum(l => l.TotalDebitos));
        movimentoCredito.Should().Be(balancete.Sum(l => l.TotalCreditos));
    }

    [Fact]
    public void SaldoInicial_e_SaldoFinal_batem_com_o_balancete()
    {
        var balancete = BalanceteBalanceado();

        var linhas = DerivadorMsc.Derivar(balancete, poderOrgao: null);

        var saldoInicial = linhas.Where(l => l.TipoValor == TipoValorMsc.SaldoInicial).Sum(l => l.Valor);
        var saldoFinal = linhas.Where(l => l.TipoValor == TipoValorMsc.SaldoFinal).Sum(l => l.Valor);

        saldoInicial.Should().Be(balancete.Sum(l => l.SaldoAnterior));
        saldoFinal.Should().Be(balancete.Sum(l => l.SaldoAtual));
    }

    [Fact]
    public void Matriz_fecha_quando_saldo_final_devedor_iguala_credor()
    {
        var linhas = DerivadorMsc.Derivar(BalanceteBalanceado(), poderOrgao: null);

        var matriz = MatrizSaldosContabeis.Montar(Tenant, 2026, 3, TipoMatrizMsc.Agregada, linhas);

        matriz.EstaBalanceada.Should().BeTrue();
        matriz.TotalSaldoFinalDevedor.Should().Be(300m);
        matriz.TotalSaldoFinalCredor.Should().Be(300m);
        matriz.TotalSaldoFinalDevedor.Should().Be(matriz.TotalSaldoFinalCredor);
    }

    [Fact]
    public void Matriz_desbalanceada_e_bloqueada()
    {
        // Apenas a conta devedora — sem a credora correspondente, o fechamento D=C quebra.
        var desbalanceado = new List<LinhaBalancete>
        {
            Linha("1.1.1.1.01", NaturezaSaldo.Devedora, 0m, 500m, 200m, 300m),
        };
        var linhas = DerivadorMsc.Derivar(desbalanceado, poderOrgao: null);

        var acao = () => MatrizSaldosContabeis.Montar(Tenant, 2026, 3, TipoMatrizMsc.Agregada, linhas);

        acao.Should().Throw<MatrizSaldosDesbalanceadaException>();
    }

    [Fact]
    public void Registro_de_valor_zero_e_descartado()
    {
        // Conta sem movimento e sem saldo: nenhum registro deve ser emitido.
        var semMovimento = new List<LinhaBalancete>
        {
            Linha("1.1.1.1.01", NaturezaSaldo.Devedora, 0m, 0m, 0m, 0m),
        };

        var linhas = DerivadorMsc.Derivar(semMovimento, poderOrgao: null);

        linhas.Should().BeEmpty();
    }

    // ---- Regras duras do SICONFI (Regras Gerais MSC 2026, "Observacoes Importantes") ----

    // PO valido (5 digitos) em todas as linhas — o PO e obrigatorio na MSC (Regras Gerais MSC 2026, IC
    // nº1); sem ele a montagem falha antes de chegar nas regras de balanco/consistencia testadas aqui.
    private static InformacoesComplementaresMsc Ic()
        => InformacoesComplementaresMsc.Criar(poderOrgao: PoderOrgaoMsc.Executivo);

    private static LinhaMsc Reg(string conta, NaturezaSaldo lado, TipoValorMsc tipo, decimal valor)
        => LinhaMsc.Criar(conta, lado, tipo, valor, Ic())!;

    [Fact]
    public void Matriz_que_fecha_no_total_mas_desbalanceia_uma_classe_e_bloqueada()
    {
        // Total geral D=C fecha (300 D = 300 C), MAS dentro da classe PATRIMONIAL ha so devedor
        // e na ORCAMENTARIA so credor: cada classe individualmente NAO fecha. O SICONFI rejeita.
        var linhas = new[]
        {
            // Patrimonial (classe 1): so saldo final devedor.
            Reg("1.1.1.1.01", NaturezaSaldo.Devedora, TipoValorMsc.SaldoInicial, 0m + 300m),
            Reg("1.1.1.1.01", NaturezaSaldo.Devedora, TipoValorMsc.SaldoFinal, 300m),
            // Orcamentaria (classe 6): so saldo final credor — fecha o TOTAL geral, nao a classe.
            Reg("6.2.1.1.00", NaturezaSaldo.Credora, TipoValorMsc.SaldoInicial, 300m),
            Reg("6.2.1.1.00", NaturezaSaldo.Credora, TipoValorMsc.SaldoFinal, 300m),
        };

        var acao = () => MatrizSaldosContabeis.Montar(Tenant, 2026, 3, TipoMatrizMsc.Agregada, linhas);

        acao.Should().Throw<MatrizSaldosClasseDesbalanceadaException>();
    }

    [Fact]
    public void Conta_com_saldo_inicial_mais_movimento_diferente_do_saldo_final_e_bloqueada()
    {
        // Conta devedora: SI(100) + Mov(D 500 - C 300 = +200) = 300, mas SF informado = 999. Inconsistente.
        // Par credor adicionado so para o total geral e cada classe fecharem (isola a regra de consistencia).
        var linhas = new[]
        {
            Reg("1.1.1.1.01", NaturezaSaldo.Devedora, TipoValorMsc.SaldoInicial, 100m),
            Reg("1.1.1.1.01", NaturezaSaldo.Devedora, TipoValorMsc.MovimentoPeriodo, 500m),
            Reg("1.1.1.1.01", NaturezaSaldo.Credora, TipoValorMsc.MovimentoPeriodo, 300m),
            Reg("1.1.1.1.01", NaturezaSaldo.Devedora, TipoValorMsc.SaldoFinal, 999m),
            Reg("2.1.3.1.01", NaturezaSaldo.Credora, TipoValorMsc.SaldoInicial, 999m),
            Reg("2.1.3.1.01", NaturezaSaldo.Credora, TipoValorMsc.SaldoFinal, 999m),
        };

        var acao = () => MatrizSaldosContabeis.Montar(Tenant, 2026, 3, TipoMatrizMsc.Agregada, linhas);

        acao.Should().Throw<MatrizSaldosContaInconsistenteException>();
    }

    [Fact]
    public void Linha_sem_poder_orgao_bloqueia_a_matriz()
    {
        // PO e associado a TODAS as contas do PCASP (Regras Gerais MSC 2026, IC nº1): sem PO ha rejeicao.
        var semPo = InformacoesComplementaresMsc.Criar();
        var linhas = new[]
        {
            LinhaMsc.Criar("1.1.1.1.01", NaturezaSaldo.Devedora, TipoValorMsc.SaldoFinal, 300m, semPo)!,
            LinhaMsc.Criar("2.1.3.1.01", NaturezaSaldo.Credora, TipoValorMsc.SaldoFinal, 300m, semPo)!,
        };

        var acao = () => MatrizSaldosContabeis.Montar(Tenant, 2026, 3, TipoMatrizMsc.Agregada, linhas);

        acao.Should().Throw<MatrizSaldosSemPoderOrgaoException>();
    }

    [Fact]
    public void Derivacao_sem_po_assume_o_executivo()
    {
        // A MSC e enviada SOMENTE pelo Executivo: na ausencia de PO, todas as linhas saem com 01001.
        var linhas = DerivadorMsc.Derivar(BalanceteBalanceado(), poderOrgao: null);

        linhas.Should().OnlyContain(l => l.Complementares.PoderOrgao == PoderOrgaoMsc.Executivo);
    }

    [Fact]
    public void Po_fora_do_formato_de_cinco_digitos_e_rejeitado()
    {
        var acao = () => InformacoesComplementaresMsc.Criar(poderOrgao: "123");

        acao.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("1.1.1.1.1", true)]   // 5o digito = 1 (Consolidacao) -> indicador valido
    [InlineData("1.1.1.1.5", true)]   // 5o digito = 5 (Inter-OFSS Municipios) -> valido
    [InlineData("1.1.1.1.7", false)]  // 5o digito = 7 -> subtitulo do Estendido, NAO consolidacao
    [InlineData("6.2.1.1.1", false)]  // classe orcamentaria -> nao patrimonial, sem consolidacao
    public void Quinto_digito_consolidacao_so_e_valido_entre_1_e_5(string codigo, bool esperado)
    {
        CodigoContabil.De(codigo).EhContaDeConsolidacao().Should().Be(esperado);
    }
}
