using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.TabelasLegais;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Conformidade do IRRF 2026 (Lei 15.270/2025): o redutor mensal amplia a isencao efetiva ate R$ 5.000,00
/// e decresce ate R$ 7.350,00. Sem ele (bug original), a competencia 2026 herdava mai-dez/2025 e retinha
/// IRRF de quem e isento. Aqui validamos a tabela 2026 OFICIAL semeada (<see cref="TabelasFederaisSeed"/>).
/// </summary>
public sealed class Irrf2026RedutorTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static TabelaIrrf Irrf2026()
        => TabelasFederaisSeed.Irrf(Tenant).Single(t => t.VigenciaInicio == Competencia.De(2026, 1));

    [Fact] // Ate R$ 5.000,00 o redutor zera o IRRF (isencao efetiva) — sem deducoes, sem dependentes.
    public void Rendimento_ate_5000_fica_isento_em_2026()
    {
        var imposto = Irrf2026().CalcularImposto(
            rendimentoTributavel: 5000m,
            descontoPrevidenciario: 0m,
            quantidadeDependentes: 0,
            pensaoAlimenticia: 0m);

        imposto.Should().Be(0m);
    }

    [Fact] // Sem o redutor a mesma base de R$ 5.000 reteria IRRF > 0 (prova que o redutor esta acoplado).
    public void Sem_redutor_a_base_5000_reteria_imposto()
    {
        // Tabela 2025 (mesma grade, sem redutor) retem para 5.000.
        var imposto2025 = TabelasFederaisSeed.Irrf(Tenant)
            .Single(t => t.VigenciaInicio == Competencia.De(2025, 5))
            .CalcularImposto(5000m, 0m, 0, 0m);

        imposto2025.Should().BeGreaterThan(0m);
    }

    [Fact] // Acima de R$ 7.350,00 nao ha redutor: o imposto e o da tabela progressiva cheia.
    public void Rendimento_acima_de_7350_nao_recebe_redutor()
    {
        var tabela = Irrf2026();
        var imposto = tabela.CalcularImposto(8000m, 0m, 0, 0m);

        // Igual ao imposto sem redutor (faixa de 27,5% menos parcela), pois redutor = 0 acima do limite.
        var faixaTopo = tabela.Faixas[^1];
        var esperado = faixaTopo.Imposto(8000m - tabela.DescontoSimplificado);
        imposto.Should().Be(esperado);
        imposto.Should().BeGreaterThan(0m);
    }

    [Theory] // Banda decrescente: redutor = min(312,89; 978,62 - 0,133145 x R), limitado ao imposto.
    [InlineData(6000)]
    [InlineData(7000)]
    public void Banda_decrescente_aplica_redutor_parcial(decimal rendimento)
    {
        var tabela = Irrf2026();
        var comRedutor = tabela.CalcularImposto(rendimento, 0m, 0, 0m);
        var semRedutor = tabela.Faixas.First(f => f.Enquadra(rendimento - tabela.DescontoSimplificado))
            .Imposto(rendimento - tabela.DescontoSimplificado);

        comRedutor.Should().BeLessThan(semRedutor); // houve reducao
        comRedutor.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact] // O redutor nunca torna o imposto negativo (limitado ao proprio imposto — art. 3o-A, § 1o).
    public void Redutor_nunca_gera_imposto_negativo()
    {
        var redutor = RedutorIrrf.De(978.62m, 0.133145m, 312.89m, 7350.00m);
        redutor.Calcular(5200m).Should().BeLessThanOrEqualTo(312.89m);
        redutor.Calcular(7400m).Should().Be(0m); // acima do limite
        redutor.Calcular(0m).Should().Be(0m);
    }
}
