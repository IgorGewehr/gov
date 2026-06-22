using FluentAssertions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Demonstracoes;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Demonstrações DCASP (read-models derivados do balancete): cada demonstração soma corretamente os
/// saldos por classe/atributo e fecha o invariante (BP Ativo=Passivo+PL; DVP Resultado=VPA−VPD;
/// BO/BF totalizam o realizado/empenhado).
/// </summary>
public sealed class DemonstracoesDcaspTests
{
    private const int Exercicio = 2026;
    private const int Mes = 6;
    private static readonly Guid Tenant = Guid.Parse("88888888-8888-8888-8888-888888888888");

    // Balancete sintético de uma competência fechada, cobrindo classes 1,2,3,4,6.
    // Patrimonial fecha: Ativo(1)=700 ; Passivo+PL(2)=700 (PC 200 + PNC 100 + PL 400).
    // Resultado patrimonial: VPA(4)=500 − VPD(3)=300 = 200 (compõe o PL).
    private static IReadOnlyList<LinhaBalancete> Balancete() =>
    [
        Conta("1.1.1.1.01", NaturezaSaldo.Devedora, 500m),  // Ativo Circulante (financeiro)
        Conta("1.2.1.1.01", NaturezaSaldo.Devedora, 200m),  // Ativo Não Circulante (permanente)
        Conta("2.1.3.1.01", NaturezaSaldo.Credora, 200m),   // Passivo Circulante (financeiro)
        Conta("2.2.1.1.01", NaturezaSaldo.Credora, 100m),   // Passivo Não Circulante (permanente)
        Conta("2.3.1.1.01", NaturezaSaldo.Credora, 400m),   // Patrimônio Líquido
        Conta("3.3.2.1.01", NaturezaSaldo.Devedora, 300m),  // VPD
        Conta("4.1.1.1.01", NaturezaSaldo.Credora, 500m),   // VPA
        Conta("6.2.1.2.01", NaturezaSaldo.Credora, 900m),   // Receita realizada
        Conta("6.2.2.1.3.03.00", NaturezaSaldo.Credora, 700m), // Despesa empenhada (estágio)
    ];

    private static LinhaBalancete Conta(string codigo, NaturezaSaldo natureza, decimal saldo)
        => new()
        {
            TenantId = Tenant,
            ContaId = Guid.NewGuid(),
            CodigoConta = codigo,
            Titulo = codigo,
            NaturezaSaldo = natureza,
            NaturezaInformacao = NaturezaInformacao.Patrimonial,
            Nivel = 5,
            Exercicio = Exercicio,
            PeriodoMes = Mes,
            SaldoAnterior = 0m,
            TotalDebitos = natureza == NaturezaSaldo.Devedora ? saldo : 0m,
            TotalCreditos = natureza == NaturezaSaldo.Credora ? saldo : 0m,
            SaldoAtual = saldo,
        };

    private static DemonstrativoContexto NovoContexto()
        => new(new BalanceteFake(Balancete()), new ContasFake());

    [Fact]
    public async Task BalancoPatrimonial_fecha_ativo_igual_passivo_mais_pl()
    {
        var handler = new GerarBalancoPatrimonialHandler(NovoContexto());

        var bp = await handler.Handle(new GerarBalancoPatrimonialQuery(Exercicio, Mes), CancellationToken.None);

        bp.TotalAtivo.Should().Be(700m);
        bp.TotalPassivoPl.Should().Be(700m);
        bp.TotalAtivo.Should().Be(bp.TotalPassivoPl);
    }

    [Fact]
    public async Task BalancoPatrimonial_apura_superavit_financeiro_pelo_indicador_fp()
    {
        var handler = new GerarBalancoPatrimonialHandler(NovoContexto());

        var bp = await handler.Handle(new GerarBalancoPatrimonialQuery(Exercicio, Mes), CancellationToken.None);

        // Ativo financeiro (1.1.* = F) = 500 ; Passivo financeiro (2.1.* = F) = 200.
        bp.AtivoFinanceiro.Should().Be(500m);
        bp.PassivoFinanceiro.Should().Be(200m);
        bp.SuperavitFinanceiro.Should().Be(300m);
    }

    [Fact]
    public async Task Dvp_resultado_patrimonial_e_vpa_menos_vpd()
    {
        var handler = new GerarDvpHandler(NovoContexto());

        var dvp = await handler.Handle(new GerarDvpQuery(Exercicio, Mes), CancellationToken.None);

        dvp.TotalVpa.Should().Be(500m);
        dvp.TotalVpd.Should().Be(300m);
        dvp.ResultadoPatrimonial.Should().Be(200m);
        // A quebra de linhas (detalhadas + Outras residual) deve somar o total da classe.
        dvp.VariacoesAumentativas.Sum(l => l.Valores[0]).Should().Be(dvp.TotalVpa);
        dvp.VariacoesDiminutivas.Sum(l => l.Valores[0]).Should().Be(dvp.TotalVpd);
    }

    [Fact]
    public async Task BalancoOrcamentario_totaliza_receita_realizada_e_despesa_empenhada()
    {
        var handler = new GerarBalancoOrcamentarioHandler(NovoContexto());

        var bo = await handler.Handle(new GerarBalancoOrcamentarioQuery(Exercicio, Mes), CancellationToken.None);

        bo.TotalReceitaRealizada.Should().Be(900m);
        bo.TotalDespesaEmpenhada.Should().Be(700m);
        bo.ResultadoOrcamentario.Should().Be(200m);
    }

    [Fact]
    public async Task BalancoFinanceiro_soma_ingressos_e_dispendios()
    {
        var handler = new GerarBalancoFinanceiroHandler(NovoContexto());

        var bf = await handler.Handle(new GerarBalancoFinanceiroQuery(Exercicio, Mes), CancellationToken.None);

        // Ingressos: receita orçamentária (6.2.1.2 = 900) + saldo de abertura (1.1.1 = 500) = 1400.
        bf.TotalIngressos.Should().Be(1400m);
        // Dispêndios: despesa orçamentária (6.2.2.1.3 = 700).
        bf.TotalDispendios.Should().Be(700m);
        bf.Ingressos.Should().NotBeEmpty();
        bf.Dispendios.Should().NotBeEmpty();
    }

    private sealed class BalanceteFake(IReadOnlyList<LinhaBalancete> linhas) : IBalanceteProjection
    {
        public Task<LinhaBalancete> ObterOuCriarLinhaAsync(ContaContabil conta, int exercicio, int periodoMes, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<LinhaBalancete>> ListarPorPeriodoAsync(int exercicio, int periodoMes, CancellationToken cancellationToken)
            => Task.FromResult(linhas);

        public Task<IReadOnlyList<LinhaBalancete>> ListarRazaoContaAsync(Guid contaId, int exercicio, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    // Reproduz os atributos F/P das contas do balancete: 1.1/2.1 = Financeiro; 1.2/2.2/2.3 = Permanente.
    private sealed class ContasFake : IContaContabilRepository
    {
        public void Adicionar(ContaContabil conta) => throw new NotSupportedException();

        public Task<ContaContabil?> ObterPorCodigoAsync(string codigo, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<ContaContabil?> ObterPorIdAsync(ContaContabilId id, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<ContaContabil>> ListarTodasAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<ContaContabil> contas =
            [
                Criar("1.1.1.1.01", IndicadorSuperavitFinanceiro.Financeiro),
                Criar("1.2.1.1.01", IndicadorSuperavitFinanceiro.Permanente),
                Criar("2.1.3.1.01", IndicadorSuperavitFinanceiro.Financeiro),
                Criar("2.2.1.1.01", IndicadorSuperavitFinanceiro.Permanente),
                Criar("2.3.1.1.01", IndicadorSuperavitFinanceiro.Permanente),
                Criar("3.3.2.1.01", IndicadorSuperavitFinanceiro.NaoAplicavel),
                Criar("4.1.1.1.01", IndicadorSuperavitFinanceiro.NaoAplicavel),
                Criar("6.2.1.2.01", IndicadorSuperavitFinanceiro.NaoAplicavel),
                Criar("6.2.2.1.3.03.00", IndicadorSuperavitFinanceiro.NaoAplicavel),
            ];
            return Task.FromResult(contas);
        }

        private static ContaContabil Criar(string codigo, IndicadorSuperavitFinanceiro indicador)
            => ContaContabil.Criar(
                Tenant,
                CodigoContabil.De(codigo),
                $"Conta {codigo}",
                "Funcao.",
                "Funcionamento.",
                TipoConta.Analitica,
                contaPaiId: null,
                indicador,
                encerramento: false);
    }
}
