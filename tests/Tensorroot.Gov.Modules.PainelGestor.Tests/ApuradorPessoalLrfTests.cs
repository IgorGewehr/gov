using FluentAssertions;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Limites;
using Xunit;

namespace Tensorroot.Gov.Modules.PainelGestor.Tests;

/// <summary>
/// Testes da regra de domínio do limite de Despesa com Pessoal da LRF (LC 101/2000): cálculo do % da RCL
/// e classificação do semáforo frente aos limites legal/prudencial/alerta. Determinístico e sem relógio.
/// </summary>
public sealed class ApuradorPessoalLrfTests
{
    private static readonly LimitesPessoalLrf Limites = LimitesPessoalLrf.PadraoExecutivoMunicipal; // 54% / 51,3% / 48,6%

    [Fact]
    public void Calcula_percentual_da_rcl_corretamente()
    {
        // 540.000 / 1.000.000 = 54% — exatamente no teto legal.
        var apuracao = ApuradorPessoalLrf.Apurar(540_000m, 1_000_000m, Limites);

        apuracao.PercentualDaRcl.Should().Be(0.54m);
        apuracao.Rcl.Should().Be(1_000_000m);
        apuracao.DespesaPessoal.Should().Be(540_000m);
    }

    [Fact]
    public void Abaixo_do_alerta_e_adequado_verde()
    {
        // 40% < 48,6% (alerta) → verde.
        var apuracao = ApuradorPessoalLrf.Apurar(400_000m, 1_000_000m, Limites);
        apuracao.Situacao.Should().Be(SituacaoLimite.Adequado);
    }

    [Fact]
    public void Entre_alerta_e_legal_e_alerta_amarelo()
    {
        // 50% — acima do alerta (48,6%) e do prudencial (51,3%? não: 50 < 51,3) mas ainda < legal → amarelo.
        var apuracao = ApuradorPessoalLrf.Apurar(500_000m, 1_000_000m, Limites);
        apuracao.Situacao.Should().Be(SituacaoLimite.Alerta);
    }

    [Fact]
    public void No_prudencial_ainda_e_alerta_amarelo()
    {
        // 52% — acima do prudencial (51,3%) e abaixo do legal (54%) → amarelo (atenção severa).
        var apuracao = ApuradorPessoalLrf.Apurar(520_000m, 1_000_000m, Limites);
        apuracao.Situacao.Should().Be(SituacaoLimite.Alerta);
    }

    [Fact]
    public void No_limite_legal_ou_acima_e_excedido_vermelho()
    {
        ApuradorPessoalLrf.Apurar(540_000m, 1_000_000m, Limites).Situacao.Should().Be(SituacaoLimite.Excedido);
        ApuradorPessoalLrf.Apurar(600_000m, 1_000_000m, Limites).Situacao.Should().Be(SituacaoLimite.Excedido);
    }

    [Fact]
    public void Sem_rcl_e_indeterminado_sem_dividir_por_zero()
    {
        var apuracao = ApuradorPessoalLrf.Apurar(540_000m, 0m, Limites);
        apuracao.PercentualDaRcl.Should().Be(0m);
        apuracao.Situacao.Should().Be(SituacaoLimite.Indeterminado);
    }

    [Fact]
    public void Limites_parametrizaveis_alteram_a_classificacao()
    {
        // Ente com limite legal mais apertado (ex.: 30%): 40% passa a ser excedido.
        var limitesApertados = new LimitesPessoalLrf(0.30m, 0.95m, 0.90m);
        var apuracao = ApuradorPessoalLrf.Apurar(400_000m, 1_000_000m, limitesApertados);
        apuracao.Situacao.Should().Be(SituacaoLimite.Excedido);
    }

    [Fact]
    public void Fatores_prudencial_e_alerta_derivam_do_legal()
    {
        Limites.LimitePrudencial.Should().Be(0.54m * 0.95m);
        Limites.LimiteAlerta.Should().Be(0.54m * 0.90m);
    }
}
