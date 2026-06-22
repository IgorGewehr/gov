using FluentAssertions;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova do motor de cálculo do ITBI (domínio puro, determinístico) sob o Tema 1.113/STJ:
/// base = VALOR DECLARADO (presunção de veracidade); o valor venal de referência é APENAS parâmetro de
/// triagem/alerta (nunca base automática); a base só sobe via arbitramento (CTN art. 148) concluído.
/// Alíquota parametrizável (lei municipal); isenção; alíquota reduzida do SFH. Nada hardcoded.
/// </summary>
public sealed class MotorItbiTests
{
    [Fact]
    public void Base_e_sempre_o_valor_declarado_mesmo_quando_menor_que_o_venal()
    {
        // Venal R$ 300.000; declarado R$ 250.000 → base = DECLARADO 250.000 (Tema 1.113); 2% → R$ 5.000.
        var memoria = CalculadoraItbi.Calcular(
            ValorMonetario.De(300_000m),
            ValorMonetario.De(250_000m),
            aliquotaGeralPercentual: 2.0m,
            aliquotaSfhPercentual: 0.5m);

        memoria.BaseCalculo.Valor.Should().Be(250_000m);
        memoria.Origem.Should().Be(OrigemBaseCalculoItbi.Declarada);
        memoria.ProcessoArbitramentoId.Should().BeNull();
        memoria.AliquotaPercentual.Should().Be(2.0m);
        memoria.ImpostoDevido.Valor.Should().Be(5_000m);
    }

    [Fact]
    public void Base_declarada_quando_maior_que_o_venal()
    {
        // Venal R$ 200.000; declarado R$ 250.000 → base = 250.000; 2% → R$ 5.000.
        var memoria = CalculadoraItbi.Calcular(
            ValorMonetario.De(200_000m),
            ValorMonetario.De(250_000m),
            aliquotaGeralPercentual: 2.0m,
            aliquotaSfhPercentual: 0.5m);

        memoria.BaseCalculo.Valor.Should().Be(250_000m);
        memoria.Origem.Should().Be(OrigemBaseCalculoItbi.Declarada);
        memoria.HaDivergenciaReferencia.Should().BeFalse();
        memoria.ImpostoDevido.Valor.Should().Be(5_000m);
    }

    [Fact]
    public void Triagem_nao_dispara_alerta_dentro_da_margem()
    {
        // Venal 300.000, declarado 250.000, margem 20% → limite 240.000; 250.000 >= 240.000 → sem alerta.
        var memoria = CalculadoraItbi.Calcular(
            ValorMonetario.De(300_000m),
            ValorMonetario.De(250_000m),
            aliquotaGeralPercentual: 2.0m,
            aliquotaSfhPercentual: 0.5m,
            margemDivergenciaPercentual: 20m);

        memoria.HaDivergenciaReferencia.Should().BeFalse();
        memoria.BaseCalculo.Valor.Should().Be(250_000m, "a triagem nunca altera a base");
    }

    [Fact]
    public void Triagem_dispara_alerta_quando_declarado_abaixo_da_margem()
    {
        // Venal 300.000, declarado 200.000, margem 10% → limite 270.000; 200.000 < 270.000 → alerta.
        var memoria = CalculadoraItbi.Calcular(
            ValorMonetario.De(300_000m),
            ValorMonetario.De(200_000m),
            aliquotaGeralPercentual: 2.0m,
            aliquotaSfhPercentual: 0.5m,
            margemDivergenciaPercentual: 10m);

        memoria.HaDivergenciaReferencia.Should().BeTrue("declarado muito abaixo da referência");
        memoria.Origem.Should().Be(OrigemBaseCalculoItbi.Declarada);
        memoria.BaseCalculo.Valor.Should().Be(200_000m, "o alerta NÃO eleva a base de ofício");
        memoria.ImpostoDevido.Valor.Should().Be(4_000m);
    }

    [Fact]
    public void Recalculo_com_arbitramento_concluido_eleva_a_base()
    {
        // Processo concluído com base arbitrada 320.000; 2% → R$ 6.400.
        var processoId = Guid.NewGuid();
        var resultado = new ResultadoArbitramento(processoId, ValorMonetario.De(320_000m), ProcessoConcluido: true);

        var memoria = CalculadoraItbi.RecalcularComArbitramento(
            ValorMonetario.De(300_000m),
            ValorMonetario.De(200_000m),
            resultado,
            aliquotaGeralPercentual: 2.0m,
            aliquotaSfhPercentual: 0.5m);

        memoria.BaseCalculo.Valor.Should().Be(320_000m);
        memoria.Origem.Should().Be(OrigemBaseCalculoItbi.ArbitradaArt148);
        memoria.ProcessoArbitramentoId.Should().Be(processoId);
        memoria.ImpostoDevido.Valor.Should().Be(6_400m);
    }

    [Fact]
    public void Recalculo_recusa_arbitramento_nao_concluido()
    {
        // Processo NÃO concluído → o motor recusa elevar a base (só após processo, CTN art. 148).
        var resultado = new ResultadoArbitramento(Guid.NewGuid(), ValorMonetario.De(320_000m), ProcessoConcluido: false);

        var acao = () => CalculadoraItbi.RecalcularComArbitramento(
            ValorMonetario.De(300_000m),
            ValorMonetario.De(200_000m),
            resultado,
            2.0m,
            0.5m);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Isencao_reduz_o_imposto_devido()
    {
        // Base declarada 250.000 × 2% = 5.000 bruto; isenção 100% (1ª aquisição SFH, ex.) → devido 0.
        var memoria = CalculadoraItbi.Calcular(
            ValorMonetario.De(200_000m),
            ValorMonetario.De(250_000m),
            aliquotaGeralPercentual: 2.0m,
            aliquotaSfhPercentual: 0.5m,
            parametros: new ParametrosItbi(PercentualIsencao: 100m));

        memoria.ImpostoBruto.Valor.Should().Be(5_000m);
        memoria.ValorIsencao.Valor.Should().Be(5_000m);
        memoria.ImpostoDevido.Valor.Should().Be(0m);
    }

    [Fact]
    public void Aliquota_reduzida_do_sfh_e_aplicada_quando_solicitada()
    {
        // Base declarada 250.000 × alíquota SFH 0.5% = R$ 1.250 (em vez de 2%).
        var memoria = CalculadoraItbi.Calcular(
            ValorMonetario.De(200_000m),
            ValorMonetario.De(250_000m),
            aliquotaGeralPercentual: 2.0m,
            aliquotaSfhPercentual: 0.5m,
            parametros: new ParametrosItbi(UsarAliquotaSfh: true));

        memoria.AliquotaPercentual.Should().Be(0.5m);
        memoria.ImpostoDevido.Valor.Should().Be(1_250m);
    }

    [Fact]
    public void Isencao_fora_do_intervalo_rejeitada()
    {
        var acao = () => CalculadoraItbi.Calcular(
            ValorMonetario.De(200_000m),
            ValorMonetario.De(250_000m),
            2.0m,
            0.5m,
            parametros: new ParametrosItbi(PercentualIsencao: 150m));

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Margem_fora_do_intervalo_rejeitada()
    {
        var acao = () => CalculadoraItbi.Calcular(
            ValorMonetario.De(200_000m),
            ValorMonetario.De(250_000m),
            2.0m,
            0.5m,
            margemDivergenciaPercentual: 150m);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }
}
