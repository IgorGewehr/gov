using FluentAssertions;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova do motor de cálculo do ITBI (domínio puro, determinístico): base = MAIOR entre valor venal
/// de referência (motor do Imóvel/PGV) e valor declarado; alíquota parametrizável (lei municipal);
/// isenção; alíquota reduzida do SFH. Nenhuma alíquota é hardcoded.
/// </summary>
public sealed class MotorItbiTests
{
    [Fact]
    public void Base_e_o_valor_declarado_quando_maior_que_o_valor_venal()
    {
        // Venal R$ 200.000; declarado R$ 250.000 → base = 250.000; alíquota 2% → ITBI R$ 5.000.
        var memoria = CalculadoraItbi.Calcular(
            ValorMonetario.De(200_000m),
            ValorMonetario.De(250_000m),
            aliquotaGeralPercentual: 2.0m,
            aliquotaSfhPercentual: 0.5m);

        memoria.BaseCalculo.Valor.Should().Be(250_000m);
        memoria.BaseFoiValorVenal.Should().BeFalse();
        memoria.AliquotaPercentual.Should().Be(2.0m);
        memoria.ImpostoDevido.Valor.Should().Be(5_000m);
    }

    [Fact]
    public void Base_e_o_valor_venal_quando_maior_que_o_declarado()
    {
        // Venal R$ 300.000; declarado R$ 250.000 → base = 300.000; alíquota 2% → ITBI R$ 6.000.
        var memoria = CalculadoraItbi.Calcular(
            ValorMonetario.De(300_000m),
            ValorMonetario.De(250_000m),
            aliquotaGeralPercentual: 2.0m,
            aliquotaSfhPercentual: 0.5m);

        memoria.BaseCalculo.Valor.Should().Be(300_000m);
        memoria.BaseFoiValorVenal.Should().BeTrue();
        memoria.ImpostoDevido.Valor.Should().Be(6_000m);
    }

    [Fact]
    public void Base_empate_adota_o_valor_venal()
    {
        // Venal = declarado = 250.000 → base = venal (>=); alíquota 3% → ITBI R$ 7.500.
        var memoria = CalculadoraItbi.Calcular(
            ValorMonetario.De(250_000m),
            ValorMonetario.De(250_000m),
            aliquotaGeralPercentual: 3.0m,
            aliquotaSfhPercentual: 0.5m);

        memoria.BaseFoiValorVenal.Should().BeTrue();
        memoria.ImpostoDevido.Valor.Should().Be(7_500m);
    }

    [Fact]
    public void Isencao_reduz_o_imposto_devido()
    {
        // Base 250.000 × 2% = 5.000 bruto; isenção 100% (1ª aquisição SFH, ex.) → devido 0.
        var memoria = CalculadoraItbi.Calcular(
            ValorMonetario.De(200_000m),
            ValorMonetario.De(250_000m),
            aliquotaGeralPercentual: 2.0m,
            aliquotaSfhPercentual: 0.5m,
            new ParametrosItbi(PercentualIsencao: 100m));

        memoria.ImpostoBruto.Valor.Should().Be(5_000m);
        memoria.ValorIsencao.Valor.Should().Be(5_000m);
        memoria.ImpostoDevido.Valor.Should().Be(0m);
    }

    [Fact]
    public void Aliquota_reduzida_do_sfh_e_aplicada_quando_solicitada()
    {
        // Base 250.000 × alíquota SFH 0.5% = R$ 1.250 (em vez de 2%).
        var memoria = CalculadoraItbi.Calcular(
            ValorMonetario.De(200_000m),
            ValorMonetario.De(250_000m),
            aliquotaGeralPercentual: 2.0m,
            aliquotaSfhPercentual: 0.5m,
            new ParametrosItbi(UsarAliquotaSfh: true));

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
            new ParametrosItbi(PercentualIsencao: 150m));

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }
}
