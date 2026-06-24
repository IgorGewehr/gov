using FluentAssertions;
using Tensorroot.Gov.Modules.PainelGestor.Domain.Limites;
using Xunit;

namespace Tensorroot.Gov.Modules.PainelGestor.Tests;

/// <summary>
/// Testes da apuração da Despesa Total com Pessoal (DTP) pela JANELA MÓVEL de 12 meses (LRF — LC 101/2000
/// art. 18 §2º): mês de referência (competência mais recente) + 11 meses anteriores. Prova que o numerador
/// do limite NÃO é o acumulado do exercício-calendário (que zeraria em janeiro e geraria falso-verde) e que
/// a janela cruza corretamente o exercício anterior.
/// </summary>
public sealed class DespesaPessoalDozeMesesCalculatorTests
{
    [Fact]
    public void Sem_competencias_nao_apura()
    {
        var r = DespesaPessoalDozeMesesCalculator.Apurar([]);
        r.Apurada.Should().BeFalse();
        r.DespesaTotalPessoal.Should().Be(0m);
    }

    [Fact]
    public void Mes_de_referencia_e_a_competencia_mais_recente()
    {
        var r = DespesaPessoalDozeMesesCalculator.Apurar(
        [
            new CompetenciaPessoal(2026, 1, 100_000m),
            new CompetenciaPessoal(2026, 3, 120_000m),
            new CompetenciaPessoal(2026, 2, 110_000m),
        ]);

        r.AnoReferencia.Should().Be(2026);
        r.MesReferencia.Should().Be(3);
    }

    [Fact]
    public void Janela_de_12_meses_cruza_o_exercicio_anterior()
    {
        // Referência = fev/2026. Janela = mar/2025 .. fev/2026. Inclui jan-fev/2026 e mar-dez/2025;
        // EXCLUI jan-fev/2025 (fora dos 12 meses).
        var competencias = new List<CompetenciaPessoal>
        {
            new(2025, 1, 999_000m), // fora da janela (anterior a mar/2025)
            new(2025, 2, 999_000m), // fora da janela
        };
        for (var mes = 3; mes <= 12; mes++)
        {
            competencias.Add(new CompetenciaPessoal(2025, mes, 50_000m)); // 10 meses * 50k = 500k
        }

        competencias.Add(new CompetenciaPessoal(2026, 1, 60_000m));
        competencias.Add(new CompetenciaPessoal(2026, 2, 70_000m));

        var r = DespesaPessoalDozeMesesCalculator.Apurar(competencias);

        r.AnoReferencia.Should().Be(2026);
        r.MesReferencia.Should().Be(2);
        r.DespesaTotalPessoal.Should().Be(500_000m + 60_000m + 70_000m); // 630k — sem os 2 meses fora da janela
    }

    [Fact]
    public void Inicio_do_ano_nao_subdimensiona_quando_ha_historico_do_ano_anterior()
    {
        // Em fevereiro, com 12 meses de ~50k cada (jan/2025..dez/2025 + jan-fev/2026 substituindo jan-fev/2025),
        // a DTP de 12 meses ~600k — NÃO os ~100k que o acumulado do exercício-calendário daria em fevereiro.
        var competencias = new List<CompetenciaPessoal>();
        for (var mes = 3; mes <= 12; mes++)
        {
            competencias.Add(new CompetenciaPessoal(2025, mes, 50_000m));
        }

        competencias.Add(new CompetenciaPessoal(2026, 1, 50_000m));
        competencias.Add(new CompetenciaPessoal(2026, 2, 50_000m));

        var r = DespesaPessoalDozeMesesCalculator.Apurar(competencias);
        r.DespesaTotalPessoal.Should().Be(600_000m); // 12 * 50k — janela cheia, não o acumulado do ano.
    }
}
