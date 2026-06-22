using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.TabelasLegais;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Calculo;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Cobertura do <see cref="MotorDeCalculoFolha"/> com CASOS CONHECIDOS conferidos contra as tabelas
/// oficiais 2026 (INSS Port. MPS/MF nº 13/2026) e IRRF mai-dez/2025 (Lei 15.191/2025). Verifica a
/// mecanica cumulativa do INSS, a regra do mais vantajoso do IRRF, o fail-closed do RPPS sem tabela
/// municipal e o determinismo. Os numeros sao DADO parametrizado (nunca hardcoded no motor — S16).
/// </summary>
public sealed class MotorDeCalculoFolhaTests
{
    private static readonly Guid Tenant = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static TabelaInss Inss2026()
        => TabelasFederaisSeed.Inss(Tenant).Single(t => t.VigenciaInicio.Ano == 2026);

    private static TabelaIrrf IrrfMaiDez2025()
        => TabelasFederaisSeed.Irrf(Tenant).Single(t => t.VigenciaInicio.Mes == 5);

    private static VerbaCalculo Vencimento(decimal valor)
        => new(Rubrica.De("VENCIMENTO"), EhProvento: true, valor, IncideInss: true, IncideRpps: true, IncideIrrf: true);

    // ---------- INSS (RGPS) progressivo cumulativo ----------

    [Theory] // Valores conferidos: faixas 7,5/9/12/14% sobre as parcelas, teto R$ 8.475,55 (2026).
    [InlineData(1500, 112.50)]
    [InlineData(3000, 248.60)]
    [InlineData(5000, 501.51)]
    [InlineData(8000, 921.51)]
    [InlineData(10000, 988.09)] // acima do teto: incide so ate 8.475,55.
    public void Inss_progressivo_cumulativo_confere_casos_conhecidos(decimal salario, decimal inssEsperado)
    {
        var resultado = MotorDeCalculoFolha.Calcular(
            new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rgps, 0, 0m, new[] { Vencimento(salario) }),
            Inss2026(),
            IrrfMaiDez2025(),
            tabelaRpps: null);

        resultado.DescontoInss.Should().Be(inssEsperado);
    }

    [Fact] // Caso conhecido completo RGPS: 5000, 0 dependentes.
    public void Caso_conhecido_rgps_5000_sem_dependentes()
    {
        var resultado = MotorDeCalculoFolha.Calcular(
            new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rgps, 0, 0m, new[] { Vencimento(5000m) }),
            Inss2026(),
            IrrfMaiDez2025(),
            tabelaRpps: null);

        resultado.DescontoInss.Should().Be(501.51m);
        resultado.DescontoIrrf.Should().Be(312.89m); // base mais vantajosa (simplificado) na faixa 22,5%.
        resultado.Liquido.Should().Be(4185.60m);
    }

    [Fact] // Caso conhecido RGPS: 3000 com 1 dependente -> IRRF zera pela base mais vantajosa.
    public void Caso_conhecido_rgps_3000_um_dependente()
    {
        var resultado = MotorDeCalculoFolha.Calcular(
            new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rgps, 1, 0m, new[] { Vencimento(3000m) }),
            Inss2026(),
            IrrfMaiDez2025(),
            tabelaRpps: null);

        resultado.DescontoInss.Should().Be(248.60m);
        resultado.DescontoIrrf.Should().Be(0m);
        resultado.Liquido.Should().Be(2751.40m);
    }

    [Fact] // Caso conhecido RGPS: 8000 com 2 dependentes.
    public void Caso_conhecido_rgps_8000_dois_dependentes()
    {
        var resultado = MotorDeCalculoFolha.Calcular(
            new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rgps, 2, 0m, new[] { Vencimento(8000m) }),
            Inss2026(),
            IrrfMaiDez2025(),
            tabelaRpps: null);

        resultado.DescontoInss.Should().Be(921.51m);
        resultado.DescontoIrrf.Should().Be(933.58m);
        resultado.Liquido.Should().Be(6144.91m);
    }

    // ---------- RPPS (lei municipal, fail-closed) ----------

    [Fact] // Fail-closed: servidor efetivo sem tabela RPPS municipal NAO calcula (S16).
    public void Rpps_sem_tabela_municipal_recusa_calculo()
    {
        var acao = () => MotorDeCalculoFolha.Calcular(
            new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rpps, 0, 0m, new[] { Vencimento(5000m) }),
            Inss2026(),
            IrrfMaiDez2025(),
            tabelaRpps: null);

        acao.Should().Throw<CalculoFolhaException>().WithMessage("*RPPS*");
    }

    [Fact] // RPPS progressivo municipal (exemplo parametrizado: 11% ate 3000, 14% acima).
    public void Rpps_progressivo_municipal_confere_caso_conhecido()
    {
        var rpps = TabelaRpps.Criar(
            Tenant,
            Competencia.De(2026, 1),
            new[]
            {
                FaixaProgressiva.De(0m, 3000m, 0.11m),
                FaixaProgressiva.De(3000m, 999999m, 0.14m),
            },
            teto: null,
            baseLegal: "Lei Municipal de Previdencia (exemplo de teste)");

        var resultado = MotorDeCalculoFolha.Calcular(
            new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rpps, 0, 0m, new[] { Vencimento(5000m) }),
            tabelaInss: null,
            IrrfMaiDez2025(),
            rpps);

        // 11%*3000 + 14%*2000 = 330 + 280 = 610.
        resultado.DescontoRpps.Should().Be(610m);
        resultado.DescontoInss.Should().Be(0m); // efetivo nao paga INSS.
        // IRRF mais vantajoso: base completa 5000-610=4390 < base simplificada 4392,80 -> usa 4390.
        // 4390 * 22,5% - 675,49 = 312,26.
        resultado.DescontoIrrf.Should().Be(312.26m);
        resultado.Liquido.Should().Be(5000m - 610m - 312.26m);
    }

    [Fact] // RGPS sem tabela INSS tambem e fail-closed.
    public void Rgps_sem_tabela_inss_recusa_calculo()
    {
        var acao = () => MotorDeCalculoFolha.Calcular(
            new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rgps, 0, 0m, new[] { Vencimento(5000m) }),
            tabelaInss: null,
            IrrfMaiDez2025(),
            tabelaRpps: null);

        acao.Should().Throw<CalculoFolhaException>();
    }

    // ---------- Determinismo e incidencias ----------

    [Fact] // Deterministico: mesmas entradas e tabelas -> mesmo resultado.
    public void Calculo_e_deterministico()
    {
        var insumos = new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rgps, 1, 0m, new[] { Vencimento(6000m) });

        var a = MotorDeCalculoFolha.Calcular(insumos, Inss2026(), IrrfMaiDez2025(), null);
        var b = MotorDeCalculoFolha.Calcular(insumos, Inss2026(), IrrfMaiDez2025(), null);

        a.Should().Be(b);
    }

    [Fact] // Base por incidencia: rubrica sem IncideInss NAO entra na base do INSS.
    public void Base_somada_por_incidencia_nao_por_nome()
    {
        var verbas = new[]
        {
            new VerbaCalculo(Rubrica.De("VENCIMENTO"), EhProvento: true, 4000m, IncideInss: true, IncideRpps: false, IncideIrrf: true),
            // Auxilio nao tributavel/sem incidencia: aumenta proventos mas nao a base de INSS/IRRF.
            new VerbaCalculo(Rubrica.De("AUX-ALIM"), EhProvento: true, 1000m, IncideInss: false, IncideRpps: false, IncideIrrf: false),
        };

        var resultado = MotorDeCalculoFolha.Calcular(
            new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rgps, 0, 0m, verbas),
            Inss2026(),
            IrrfMaiDez2025(),
            null);

        resultado.TotalProventos.Should().Be(5000m);
        resultado.BaseInss.Should().Be(4000m); // so o vencimento incide.
        resultado.BaseIrrf.Should().Be(4000m);
    }

    [Fact] // Liquido nunca negativo: descontos manuais acima dos proventos consolidam zero.
    public void Liquido_nunca_negativo()
    {
        var verbas = new[]
        {
            new VerbaCalculo(Rubrica.De("VENCIMENTO"), EhProvento: true, 1000m, IncideInss: true, IncideRpps: false, IncideIrrf: true),
            new VerbaCalculo(Rubrica.De("PENSAO-JUD"), EhProvento: false, 2000m, IncideInss: false, IncideRpps: false, IncideIrrf: false),
        };

        var resultado = MotorDeCalculoFolha.Calcular(
            new InsumosCalculoServidor(Guid.NewGuid(), RegimePrevidenciario.Rgps, 0, 0m, verbas),
            Inss2026(),
            IrrfMaiDez2025(),
            null);

        resultado.Liquido.Should().Be(0m);
    }
}
