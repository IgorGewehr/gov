using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.TabelasLegais;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Calculo;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.CicloAnual;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura PURA (sem I/O) do ciclo anual da folha: avos por datas (regra dos 15 dias, admissao no meio
/// do ano), 13o (proporcionalidade + 2a parcela com INSS/IRRF PROPRIOS em base separada, IRRF sem
/// simplificado — art. 12-A), ferias + 1/3 e composicao de verbas rescisorias por tipo de desligamento.
/// Numeros sao DADO parametrizado (tabelas oficiais); o calculo e funcao pura de datas/entradas (S16).
/// </summary>
public sealed class CicloAnualDominioTests
{
    private static readonly Guid Tenant = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static TabelaInss Inss2026()
        => TabelasFederaisSeed.Inss(Tenant).Single(t => t.VigenciaInicio.Ano == 2026);

    private static TabelaIrrf IrrfMaiDez2025()
        => TabelasFederaisSeed.Irrf(Tenant).Single(t => t.VigenciaInicio.Mes == 5);

    // ---------- Avos (regra dos 15 dias) ----------

    [Fact] // Ano completo: 12 avos.
    public void Avos_ano_completo_da_doze()
    {
        var avos = Avos.Apurar(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), []);
        avos.Quantidade.Should().Be(12);
    }

    [Theory] // Admissao no meio do ano: mes de admissao conta se houver >=15 dias trabalhados.
    [InlineData(2026, 4, 10, 9)]  // abril 10..30 = 21 dias (>=15 -> conta) + mai..dez (8) = 9.
    [InlineData(2026, 4, 20, 8)]  // abril 20..30 = 11 dias (<15 -> nao conta) + mai..dez (8) = 8.
    [InlineData(2026, 1, 16, 12)] // jan 16..31 = 16 dias (>=15) -> conta + fev..dez (11) = 12.
    [InlineData(2026, 1, 18, 11)] // jan 18..31 = 14 dias (<15 -> nao conta) + fev..dez (11) = 11.
    public void Avos_admissao_no_meio_do_ano_aplica_regra_dos_15_dias(int ano, int mes, int dia, int esperado)
    {
        var avos = Avos.Apurar(new DateOnly(ano, mes, dia), new DateOnly(2026, 12, 31), []);
        avos.Quantidade.Should().Be(esperado);
    }

    [Fact] // Afastamento que suspende a contagem reduz os dias do mes (pode derrubar o avo).
    public void Avos_afastamento_que_suspende_reduz_dias_do_mes()
    {
        // Marco inteiro afastado (suspende): marco nao conta. Demais meses do ano contam.
        var afastamentos = new[] { new IntervaloAfastamento(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), SuspendeContagem: true) };
        var avos = Avos.Apurar(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), afastamentos);
        avos.Quantidade.Should().Be(11);
    }

    // ---------- 13o salario ----------

    [Fact] // 13o proporcional: admissao em abril (9 avos) sobre vencimento 3000 -> (3000/12)*9 = 2250.
    public void Decimo_terceiro_proporcional_por_avos_admissao_no_meio_do_ano()
    {
        var avos = Avos.Apurar(new DateOnly(2026, 4, 10), new DateOnly(2026, 12, 31), []);
        var integral = CalculadoraDecimoTerceiro.CalcularIntegral(3000m, avos);
        integral.Should().Be(2250m);
    }

    [Fact] // 1a parcela: 50% (param) do integral, sem descontos.
    public void Decimo_terceiro_primeira_parcela_e_metade_sem_desconto()
    {
        var parcela = CalculadoraDecimoTerceiro.CalcularParcela(3000m, 0.5m);
        parcela.Should().Be(1500m);
    }

    [Fact] // 2a parcela: INSS/IRRF PROPRIOS do 13o em BASE SEPARADA; IRRF sem simplificado (art. 12-A).
    public void Decimo_terceiro_segunda_parcela_apura_inss_e_irrf_proprios_base_separada()
    {
        // 13o integral 5000 (12 avos sobre 5000). Base separada: INSS 501,51; IRRF sem simplificado.
        var verba = new VerbaCalculo(Rubrica.De("13-SAL"), EhProvento: true, 5000m, IncideInss: true, IncideRpps: false, IncideIrrf: true);
        var insumos = new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rgps, 0, 0m, [verba]);

        var resultado = MotorDeCalculoFolha.CalcularBaseSeparada(
            insumos, Inss2026(), IrrfMaiDez2025(), tabelaRpps: null, OpcoesIrrf.DecimoTerceiro);

        resultado.DescontoInss.Should().Be(501.51m);
        // IRRF do 13o NAO usa o desconto simplificado: base = 5000 - 501,51 = 4498,49; faixa 22,5% - 675,49.
        resultado.DescontoIrrf.Should().Be(336.67m);
    }

    [Fact] // Contraste: a folha MENSAL (com simplificado) daria IRRF MENOR para o mesmo 5000 (312,89).
    public void Irrf_mensal_com_simplificado_difere_do_irrf_do_decimo_terceiro()
    {
        var verba = new VerbaCalculo(Rubrica.De("VENCIMENTO"), EhProvento: true, 5000m, IncideInss: true, IncideRpps: false, IncideIrrf: true);
        var insumos = new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rgps, 0, 0m, [verba]);

        var mensal = MotorDeCalculoFolha.Calcular(insumos, Inss2026(), IrrfMaiDez2025(), tabelaRpps: null);
        mensal.DescontoIrrf.Should().Be(312.89m);
        mensal.DescontoIrrf.Should().BeLessThan(336.67m); // o 13o tributa mais (sem simplificado).
    }

    // ---------- Ferias ----------

    [Fact] // Ferias 30 dias gozados sobre 3000 + 1/3 = 3000 + 1000.
    public void Ferias_remuneracao_mais_um_terco_constitucional()
    {
        var ferias = CalculadoraFerias.Calcular(3000m, diasGozados: 30, diasVendidos: 0, fracaoTerco: 1m / 3m);
        ferias.RemuneracaoFerias.Should().Be(3000m);
        ferias.TercoConstitucional.Should().Be(1000m);
        ferias.AbonoPecuniario.Should().Be(0m);
    }

    [Fact] // Abono pecuniario: 10 dias vendidos sobre 3000 = 1000 + terco 333,33.
    public void Ferias_abono_pecuniario_inclui_terco_proporcional()
    {
        var ferias = CalculadoraFerias.Calcular(3000m, diasGozados: 20, diasVendidos: 10, fracaoTerco: 1m / 3m);
        ferias.RemuneracaoFerias.Should().Be(2000m);  // 20 dias.
        ferias.AbonoPecuniario.Should().Be(1000m);    // 10 dias.
        ferias.TercoAbono.Should().Be(333.33m);
    }

    // ---------- Rescisao (matriz por tipo x regime) ----------

    [Fact] // Dispensa sem justa causa (celetista): todas as verbas + aviso + multa FGTS.
    public void Rescisao_dispensa_sem_justa_causa_celetista_inclui_aviso_e_multa()
    {
        var verbas = CompositorRescisao.Compor(TipoDesligamento.DispensaSemJustaCausa, RegimeVinculo.Celetista);
        verbas.Should().Contain(new[]
        {
            VerbaRescisoria.SaldoSalario, VerbaRescisoria.DecimoTerceiroProporcional,
            VerbaRescisoria.FeriasVencidas, VerbaRescisoria.FeriasProporcionais,
            VerbaRescisoria.AvisoPrevio, VerbaRescisoria.MultaFgts,
        });
    }

    [Fact] // Justa causa: so saldo + ferias vencidas + 1/3; sem 13o proporcional, ferias proporcionais, aviso ou multa.
    public void Rescisao_justa_causa_nao_paga_decimo_proporcional_nem_ferias_proporcionais()
    {
        var verbas = CompositorRescisao.Compor(TipoDesligamento.JustaCausa, RegimeVinculo.Celetista);
        verbas.Should().Contain(VerbaRescisoria.SaldoSalario);
        verbas.Should().Contain(VerbaRescisoria.FeriasVencidas);
        verbas.Should().NotContain(VerbaRescisoria.DecimoTerceiroProporcional);
        verbas.Should().NotContain(VerbaRescisoria.FeriasProporcionais);
        verbas.Should().NotContain(VerbaRescisoria.AvisoPrevio);
        verbas.Should().NotContain(VerbaRescisoria.MultaFgts);
    }

    [Fact] // Estatutario (exoneracao/vacancia): sem FGTS/aviso/multa, ainda que a matriz marque-os.
    public void Rescisao_estatutario_nunca_tem_aviso_nem_multa_fgts()
    {
        var verbas = CompositorRescisao.Compor(TipoDesligamento.ExoneracaoVacancia, RegimeVinculo.Estatutario);
        verbas.Should().Contain(VerbaRescisoria.SaldoSalario);
        verbas.Should().Contain(VerbaRescisoria.DecimoTerceiroProporcional);
        verbas.Should().Contain(VerbaRescisoria.FeriasVencidas);
        verbas.Should().NotContain(VerbaRescisoria.AvisoPrevio);
        verbas.Should().NotContain(VerbaRescisoria.MultaFgts);
    }

    [Fact] // Saldo de salario: vencimento x dias/diasDoMes.
    public void Rescisao_saldo_de_salario_proporcional_aos_dias_trabalhados()
    {
        var saldo = CompositorRescisao.CalcularSaldoSalario(3000m, diasTrabalhadosNoMes: 15, diasDoMes: 30);
        saldo.Should().Be(1500m);
    }
}
