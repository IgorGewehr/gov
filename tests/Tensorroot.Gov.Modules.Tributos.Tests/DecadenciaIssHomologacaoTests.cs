using FluentAssertions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Decadência do ISS por HOMOLOGAÇÃO (R4) — [revisao-humana-juridica]. Prova a regra DUPLA:
/// CTN art. 150 §4º (homologação COM pagamento e SEM dolo: conta do FATO GERADOR) vs art. 173, I
/// (ofício, ou homologação SEM pagamento [Súmula 555/STJ], ou COM dolo: 1º dia do exercício seguinte).
/// Datas concretas; prazo 5 anos. Constituição EM/APÓS a data-limite é NULA (fail-closed).
/// </summary>
public sealed class DecadenciaIssHomologacaoTests
{
    private static readonly Guid Tenant = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateOnly FatoGerador = new(2020, 03, 10);
    private const int Anos = 5;

    // --- Regra de cálculo (CalcularDataLimiteDecadencia) ---

    [Fact]
    public void Homologacao_com_pagamento_sem_dolo_conta_do_fato_gerador_150_4()
    {
        var limite = Lancamento.CalcularDataLimiteDecadencia(
            FatoGerador, Anos, TipoLancamento.Homologacao,
            houvePagamentoAntecipado: true, doloFraudeSimulacao: false);

        limite.Should().Be(new DateOnly(2025, 03, 10), "art. 150 §4º conta do fato gerador + 5 anos");
    }

    [Fact]
    public void Homologacao_sem_pagamento_recai_no_173_I_sumula_555()
    {
        var limite = Lancamento.CalcularDataLimiteDecadencia(
            FatoGerador, Anos, TipoLancamento.Homologacao,
            houvePagamentoAntecipado: false, doloFraudeSimulacao: false);

        limite.Should().Be(new DateOnly(2026, 01, 01), "sem pagamento recai no art. 173, I (Súmula 555/STJ)");
    }

    [Fact]
    public void Homologacao_com_dolo_recai_no_173_I()
    {
        var limite = Lancamento.CalcularDataLimiteDecadencia(
            FatoGerador, Anos, TipoLancamento.Homologacao,
            houvePagamentoAntecipado: true, doloFraudeSimulacao: true);

        limite.Should().Be(new DateOnly(2026, 01, 01), "dolo/fraude/simulação afasta o §4º");
    }

    [Fact]
    public void Oficio_conta_do_exercicio_seguinte_173_I_nao_regride()
    {
        var limite = Lancamento.CalcularDataLimiteDecadencia(
            new DateOnly(2020, 01, 01), Anos, TipoLancamento.Oficio,
            houvePagamentoAntecipado: false, doloFraudeSimulacao: false);

        limite.Should().Be(new DateOnly(2026, 01, 01), "ofício (IPTU/taxas) mantém o art. 173, I");
    }

    // --- Invariante fail-closed na constituição (factory) ---

    [Fact]
    public void Iss_homologacao_constituicao_no_limite_decai()
    {
        // FG 10/03/2020 + pagamento => limite 10/03/2025. Constituir EM 10/03/2025 é NULO.
        var acao = () => Lancamento.LancarIssPorHomologacao(
            Tenant, ContribuinteId.New(), Competencia.De(2020, 3), ValorMonetario.De(1_000m),
            vencimento: new DateOnly(2020, 04, 10), dataFatoGerador: FatoGerador,
            dataConstituicao: new DateOnly(2025, 03, 10), houvePagamentoAntecipado: true);

        acao.Should().Throw<CreditoTributarioDecaidoException>();
    }

    [Fact]
    public void Iss_homologacao_constituicao_vespera_do_limite_e_valida()
    {
        var lancamento = Lancamento.LancarIssPorHomologacao(
            Tenant, ContribuinteId.New(), Competencia.De(2020, 3), ValorMonetario.De(1_000m),
            vencimento: new DateOnly(2020, 04, 10), dataFatoGerador: FatoGerador,
            dataConstituicao: new DateOnly(2025, 03, 09), houvePagamentoAntecipado: true);

        lancamento.TipoLancamento.Should().Be(TipoLancamento.Homologacao);
        lancamento.HouvePagamentoAntecipado.Should().BeTrue();
    }
}
