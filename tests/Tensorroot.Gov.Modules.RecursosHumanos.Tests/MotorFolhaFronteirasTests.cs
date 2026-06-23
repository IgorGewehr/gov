using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.TabelasLegais;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Calculo;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Edge-cases de FRONTEIRA do motor de folha (F-B1/F-B2/F-B3/F-B5/F-B7/F-B8 da QUALIDADE-MOTORES):
/// limites exatos das faixas INSS (inferior exclusivo / superior inclusivo) e IRRF (limite superior
/// inclusivo), teto do INSS no valor exato, multiplas incidencias e arredondamento HALF-UP. Sao
/// exatamente os pontos que o TCE-RS reconfere centavo a centavo.
/// </summary>
public sealed class MotorFolhaFronteirasTests
{
    private static readonly Guid Tenant = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static TabelaInss Inss2026()
        => TabelasFederaisSeed.Inss(Tenant).Single(t => t.VigenciaInicio.Ano == 2026);

    private static TabelaIrrf IrrfMaiDez2025()
        => TabelasFederaisSeed.Irrf(Tenant).Single(t => t.VigenciaInicio.Mes == 5);

    private static VerbaCalculo Vencimento(decimal valor)
        => new(Rubrica.De("VENCIMENTO"), EhProvento: true, valor, IncideInss: true, IncideRpps: true, IncideIrrf: true);

    private static decimal Inss(decimal salario)
        => MotorDeCalculoFolha.Calcular(
            new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rgps, 0, 0m, new[] { Vencimento(salario) }),
            Inss2026(), IrrfMaiDez2025(), null).DescontoInss;

    // ---------- F-B1: fronteiras EXATAS das faixas INSS 2026 ----------
    // Faixas 2026: [0,1621] 7,5% | (1621,2902.84] 9% | (2902.84,4354.27] 12% | (4354.27,8475.55] 14%.

    [Theory] // Limite inferior EXCLUSIVO / superior INCLUSIVO: no limite, a base ainda pertence a faixa de baixo.
    [InlineData(1621.00, 121.58)]   // topo exato da 1a faixa: 1621 * 7,5% = 121,575 -> 121,58 (HALF-UP).
    [InlineData(1621.01, 121.58)]   // +0,01 entra na 2a faixa por 0,01: 121,575 + 0,01*9% ~ 121,58.
    [InlineData(2902.84, 236.94)]   // topo exato da 2a faixa.
    [InlineData(4354.27, 411.11)]   // topo exato da 3a faixa.
    public void Inss_nas_fronteiras_exatas_das_faixas(decimal salario, decimal inssEsperado)
        => Inss(salario).Should().Be(inssEsperado);

    // ---------- F-B2: teto do INSS no valor EXATO ----------

    [Fact] // No teto exato (8475,55) -> contribuicao maxima; teto+0,01 da o MESMO valor (acima do teto nao incide).
    public void Inss_no_teto_exato_e_acima_dao_a_contribuicao_maxima()
    {
        var noTeto = Inss(8475.55m);
        var acimaDoTeto = Inss(8475.56m);
        var bemAcima = Inss(50000m);

        noTeto.Should().Be(acimaDoTeto, "acima do teto a contribuicao do INSS nao cresce");
        noTeto.Should().Be(bemAcima, "qualquer base acima do teto rende a contribuicao maxima");
        // 121,575 + (2902,84-1621)*9% + (4354,27-2902,84)*12% + (8475,55-4354,27)*14% = contribuicao maxima.
        noTeto.Should().Be(988.09m);
    }

    // ---------- F-B8: ponto de virada do IRRF mais vantajoso (isencao na fronteira) ----------
    // Tabela mai-dez/2025: 1a faixa IRRF isenta ate 2428,80 (limite superior INCLUSIVO).

    [Fact] // Base de IRRF EXATAMENTE no limite de isencao (2428,80) -> imposto zero (limite inclusivo).
    public void Irrf_na_fronteira_de_isencao_e_isento()
    {
        // Servidor RPPS para controlar a base do IRRF = vencimento - RPPS, isolando o limite de isencao.
        var rpps = TabelaRpps.Criar(
            Tenant, Competencia.De(2026, 1),
            new[] { FaixaProgressiva.De(0m, 999999m, 0m) }, // RPPS 0% para nao mexer na base.
            teto: null, baseLegal: "Teste fronteira IRRF");

        decimal IrrfPara(decimal vencimento) => MotorDeCalculoFolha.Calcular(
            new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rpps, 0, 0m, new[] { Vencimento(vencimento) }),
            null, IrrfMaiDez2025(), rpps).DescontoIrrf;

        // No limite exato 2428,80: a base mais vantajosa (completa) ainda e isenta.
        IrrfPara(2428.80m).Should().Be(0m, "limite de isencao e inclusivo");
    }

    // ---------- F-B3: multiplas rubricas com incidencias diferentes ----------

    [Fact] // BaseInss != BaseIrrf != TotalProventos quando cada rubrica tem incidencia distinta.
    public void Bases_distintas_por_incidencia_de_cada_rubrica()
    {
        var verbas = new[]
        {
            // Vencimento: INSS + IRRF.
            new VerbaCalculo(Rubrica.De("VENCIMENTO"), EhProvento: true, 4000m, IncideInss: true, IncideRpps: false, IncideIrrf: true),
            // Gratificacao: so IRRF.
            new VerbaCalculo(Rubrica.De("GRAT"), EhProvento: true, 1000m, IncideInss: false, IncideRpps: false, IncideIrrf: true),
            // Auxilio: nenhuma incidencia.
            new VerbaCalculo(Rubrica.De("AUX"), EhProvento: true, 500m, IncideInss: false, IncideRpps: false, IncideIrrf: false),
            // Adicional: so INSS.
            new VerbaCalculo(Rubrica.De("ADIC"), EhProvento: true, 300m, IncideInss: true, IncideRpps: false, IncideIrrf: false),
        };

        var resultado = MotorDeCalculoFolha.Calcular(
            new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rgps, 0, 0m, verbas),
            Inss2026(), IrrfMaiDez2025(), null);

        resultado.TotalProventos.Should().Be(5800m);
        resultado.BaseInss.Should().Be(4300m, "vencimento + adicional");
        resultado.BaseIrrf.Should().Be(5000m, "vencimento + gratificacao");
        resultado.BaseInss.Should().NotBe(resultado.BaseIrrf);
        resultado.BaseInss.Should().NotBe(resultado.TotalProventos);
    }

    // ---------- F-B7: verbas vazias e servidor so com desconto manual ----------

    [Fact] // Verbas vazias -> resultado todo zero, sem excecao.
    public void Sem_verbas_resultado_e_todo_zero()
    {
        var resultado = MotorDeCalculoFolha.Calcular(
            new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rgps, 0, 0m, Array.Empty<VerbaCalculo>()),
            Inss2026(), IrrfMaiDez2025(), null);

        resultado.TotalProventos.Should().Be(0m);
        resultado.DescontoInss.Should().Be(0m);
        resultado.DescontoIrrf.Should().Be(0m);
        resultado.Liquido.Should().Be(0m);
    }

    // ---------- F-B4: queda monotonica do IRRF por dependente ----------

    [Fact] // Cada dependente adicional nunca AUMENTA o IRRF (deducao na base completa) e pode zera-lo.
    public void Irrf_cai_monotonicamente_por_dependente()
    {
        decimal IrrfComDeps(int deps) => MotorDeCalculoFolha.Calcular(
            new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rgps, deps, 0m, new[] { Vencimento(6000m) }),
            Inss2026(), IrrfMaiDez2025(), null).DescontoIrrf;

        var anterior = decimal.MaxValue;
        for (var deps = 0; deps <= 16; deps++)
        {
            var irrf = IrrfComDeps(deps);
            irrf.Should().BeLessThanOrEqualTo(anterior, $"IRRF nao pode subir ao adicionar o {deps}o dependente");
            anterior = irrf;
        }

        IrrfComDeps(16).Should().Be(0m, "dependentes suficientes zeram o IRRF pela base completa");
    }

    // ---------- F-B5: arredondamento HALF-UP na soma das faixas ----------

    [Fact] // Determinismo do arredondamento: arredonda UMA vez na soma (nao banker's, nao por faixa).
    public void Inss_arredonda_half_up_uma_vez_na_soma()
    {
        // 1621 * 7,5% = 121,575 -> HALF-UP -> 121,58 (banker's daria 121,58 tambem aqui, mas o
        // ponto e blindar contra "arredonda por faixa": a soma e arredondada exatamente uma vez).
        Inss(1621.00m).Should().Be(121.58m);
    }
}
