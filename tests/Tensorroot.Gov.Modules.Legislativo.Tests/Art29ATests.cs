using FluentAssertions;
using Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// W9.5 — motor do art. 29-A da CF/88 (teto de despesa TOTAL do Legislativo municipal + subteto da
/// folha §1 + regra temporal EC 109/2021). Prova: resolucao da faixa por populacao (com a borda
/// explicita de 100.000 hab), apuracao do teto, ultrapassagem -> semaforo Excedido, subteto da folha
/// (70% do repasse) e o corte temporal dos inativos/pensionistas (so entram no teto a partir de 2025).
/// Nenhum percentual hardcoded: tudo vem de <see cref="ParametrosArt29A"/> (parametro por tenant).
/// </summary>
public sealed class Art29ATests
{
    private static readonly Guid TenantCamara = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Tabela de faixas pos-EC 58/2009 (defaults tipicos; aqui injetados como parametro, nao hardcoded no dominio).
    private static ParametrosArt29A ParametrosPadrao(int exercicioCorteInativos = 2025)
        => ParametrosArt29A.De(
            new[]
            {
                FaixaPopulacional.De(100_000, 0.07m),
                FaixaPopulacional.De(300_000, 0.06m),
                FaixaPopulacional.De(500_000, 0.05m),
                FaixaPopulacional.De(3_000_000, 0.045m),
                FaixaPopulacional.De(8_000_000, 0.04m),
                FaixaPopulacional.De(int.MaxValue, 0.035m),
            },
            subtetoFolhaSobreRepasse: 0.70m,
            limiarAtencao: 0.95m,
            exercicioCorteInativos: exercicioCorteInativos);

    [Theory] // Faixa por populacao, incluindo a BORDA de 100.000 hab (inclusive na primeira faixa).
    [InlineData(5_000, 0.07)]
    [InlineData(100_000, 0.07)]   // borda inclusive
    [InlineData(100_001, 0.06)]   // ultrapassa a borda -> proxima faixa
    [InlineData(300_000, 0.06)]
    [InlineData(450_000, 0.05)]
    [InlineData(9_000_000, 0.035)]
    public void ResolverFaixa_aplica_percentual_por_populacao(int populacao, double percentualEsperado)
    {
        var faixa = ParametrosPadrao().ResolverFaixa(populacao);

        faixa.Percentual.Should().Be((decimal)percentualEsperado);
    }

    [Fact] // Municipio pequeno (7%): despesa dentro do teto e do subteto -> tudo Adequado.
    public void Apurar_dentro_do_teto_e_adequado()
    {
        var apuracao = ApuracaoBase(populacao: 5_000, receitaTributaria: 8_000_000m, transferencias: 12_000_000m, repasse: 1_400_000m);
        // Teto = 20.000.000 * 7% = 1.400.000.
        apuracao.LancarDespesa(NaturezaDespesaCamara.PessoalAtivo, 900_000m);   // folha < 70% de 1.400.000 = 980.000
        apuracao.LancarDespesa(NaturezaDespesaCamara.OutrasDespesas, 300_000m); // total = 1.200.000 < 1.400.000

        var resultado = apuracao.Apurar();

        resultado.TetoDespesaTotal.Should().Be(1_400_000m);
        resultado.DespesaTotalRealizada.Should().Be(1_200_000m);
        resultado.SemaforoTeto.Should().Be(SemaforoLimite.Adequado);
        resultado.SubtetoFolha.Should().Be(980_000m);
        resultado.FolhaRealizada.Should().Be(900_000m);
        resultado.SemaforoFolha.Should().Be(SemaforoLimite.Adequado);
        resultado.Irregular.Should().BeFalse();
    }

    [Fact] // Ultrapassagem do teto total -> semaforo Excedido + Irregular (art. 29-A §2/§3).
    public void Apurar_acima_do_teto_total_excede()
    {
        var apuracao = ApuracaoBase(populacao: 5_000, receitaTributaria: 8_000_000m, transferencias: 12_000_000m, repasse: 1_400_000m);
        // Teto = 1.400.000. Lanca despesa total acima.
        apuracao.LancarDespesa(NaturezaDespesaCamara.OutrasDespesas, 1_500_000m);

        var resultado = apuracao.Apurar();

        resultado.DespesaTotalRealizada.Should().Be(1_500_000m);
        resultado.MargemTeto.Should().Be(-100_000m);
        resultado.SemaforoTeto.Should().Be(SemaforoLimite.Excedido);
        resultado.Irregular.Should().BeTrue();
    }

    [Fact] // Subteto da folha (§1, 70% do repasse) estourado, ainda que dentro do teto total.
    public void Apurar_folha_acima_do_subteto_excede_folha()
    {
        var apuracao = ApuracaoBase(populacao: 5_000, receitaTributaria: 8_000_000m, transferencias: 12_000_000m, repasse: 1_000_000m);
        // Subteto folha = 70% de 1.000.000 = 700.000. Teto total = 1.400.000.
        apuracao.LancarDespesa(NaturezaDespesaCamara.PessoalAtivo, 800_000m); // folha 800.000 > 700.000, total < teto

        var resultado = apuracao.Apurar();

        resultado.SubtetoFolha.Should().Be(700_000m);
        resultado.FolhaRealizada.Should().Be(800_000m);
        resultado.SemaforoFolha.Should().Be(SemaforoLimite.Excedido);
        resultado.SemaforoTeto.Should().Be(SemaforoLimite.Adequado); // 800.000 < 1.400.000
        resultado.Irregular.Should().BeTrue();
    }

    [Fact] // EC 109/2021: ANTES do corte (exercicio 2024), inativos/pensionistas NAO entram no teto.
    public void Apurar_inativos_fora_do_teto_antes_do_corte_ec109()
    {
        var apuracao = ApuracaoBase(
            exercicio: 2024,
            populacao: 5_000,
            receitaTributaria: 8_000_000m,
            transferencias: 12_000_000m,
            repasse: 1_400_000m);
        apuracao.LancarDespesa(NaturezaDespesaCamara.PessoalAtivo, 600_000m);
        apuracao.LancarDespesa(NaturezaDespesaCamara.InativosPensionistas, 500_000m); // excluido em 2024

        var resultado = apuracao.Apurar();

        resultado.InativosNoTeto.Should().BeFalse();
        resultado.DespesaTotalRealizada.Should().Be(600_000m); // inativos fora
        resultado.FolhaRealizada.Should().Be(600_000m);        // inativos fora da folha tambem
    }

    [Fact] // EC 109/2021: A PARTIR do corte (exercicio 2025), inativos/pensionistas integram o teto.
    public void Apurar_inativos_no_teto_a_partir_do_corte_ec109()
    {
        var apuracao = ApuracaoBase(
            exercicio: 2025,
            populacao: 5_000,
            receitaTributaria: 8_000_000m,
            transferencias: 12_000_000m,
            repasse: 1_400_000m);
        apuracao.LancarDespesa(NaturezaDespesaCamara.PessoalAtivo, 600_000m);
        apuracao.LancarDespesa(NaturezaDespesaCamara.InativosPensionistas, 500_000m); // incluido em 2025

        var resultado = apuracao.Apurar();

        resultado.InativosNoTeto.Should().BeTrue();
        resultado.DespesaTotalRealizada.Should().Be(1_100_000m); // 600k + 500k
        resultado.FolhaRealizada.Should().Be(1_100_000m);        // ambos sao folha
    }

    [Fact] // Consolidar congela o demonstrativo (terminal) e bloqueia novos lancamentos.
    public void Consolidar_torna_apuracao_terminal()
    {
        var apuracao = ApuracaoBase(populacao: 5_000, receitaTributaria: 8_000_000m, transferencias: 12_000_000m, repasse: 1_400_000m);
        apuracao.LancarDespesa(NaturezaDespesaCamara.OutrasDespesas, 100_000m);

        apuracao.Consolidar();

        apuracao.Situacao.Should().Be(SituacaoApuracaoArt29A.Consolidada);
        var novoLancamento = () => apuracao.LancarDespesa(NaturezaDespesaCamara.OutrasDespesas, 1m);
        novoLancamento.Should().Throw<InvalidOperationException>();
    }

    [Fact] // A base de receita DEVE ser do exercicio anterior (caput) — invariante de abertura.
    public void Abrir_exige_base_do_exercicio_anterior()
    {
        var baseErrada = ReceitaBaseArt29A.De(2025, 8_000_000m, 12_000_000m); // mesmo ano do exercicio
        var acao = () => ApuracaoArt29A.Abrir(TenantCamara, 2025, 5_000, baseErrada, 1_000_000m, ParametrosPadrao());

        acao.Should().Throw<ArgumentException>();
    }

    private static ApuracaoArt29A ApuracaoBase(
        int populacao,
        decimal receitaTributaria,
        decimal transferencias,
        decimal repasse,
        int exercicio = 2025)
    {
        var baseReceita = ReceitaBaseArt29A.De(exercicio - 1, receitaTributaria, transferencias);
        return ApuracaoArt29A.Abrir(TenantCamara, exercicio, populacao, baseReceita, repasse, ParametrosPadrao());
    }
}
