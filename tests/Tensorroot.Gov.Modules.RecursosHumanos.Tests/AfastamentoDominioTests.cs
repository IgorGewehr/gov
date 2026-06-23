using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Testes-chave do agregado <see cref="Afastamento"/> TIPADO e do seu efeito DETERMINISTICO na folha
/// (Onda 1 — corrige o risco de pagar servidor afastado como ativo). Cobrem: maternidade integral,
/// licenca sem vencimento (provento zerado), doenca >15d (ente paga 15d, resto suspenso), encerramento
/// e a regra-mestra de que o efeito vem da <see cref="RegraAfastamento"/> (nao digitado). Dominio puro.
/// </summary>
public sealed class AfastamentoDominioTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly ServidorId Servidor = new(Guid.NewGuid());
    private static readonly Competencia Junho2026 = Competencia.De(2026, 6); // 30 dias

    private static RegraAfastamento RegraMaternidade()
        => RegraAfastamento.Definir(Tenant, TipoAfastamento.LicencaMaternidade, Junho2026,
            suspendeProventos: false, percentualRemuneracao: 100m, diasPagosPeloEnte: 0, contaTempo: true, duracaoPadraoDias: 120);

    private static RegraAfastamento RegraSemVencimento()
        => RegraAfastamento.Definir(Tenant, TipoAfastamento.LicencaSemVencimento, Junho2026,
            suspendeProventos: true, percentualRemuneracao: 0m, diasPagosPeloEnte: 0, contaTempo: false, duracaoPadraoDias: null);

    private static RegraAfastamento RegraDoencaInss()
        => RegraAfastamento.Definir(Tenant, TipoAfastamento.DoencaInss, Junho2026,
            suspendeProventos: true, percentualRemuneracao: 100m, diasPagosPeloEnte: 15, contaTempo: true, duracaoPadraoDias: null);

    [Fact]
    public void Abrir_afastamento_nasce_vigente_congelando_a_regra_e_emite_evento()
    {
        var af = Afastamento.Abrir(Tenant, Servidor, TipoAfastamento.LicencaMaternidade,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 9, 28), "Portaria 10/2026", RegraMaternidade());

        af.Situacao.Should().Be(SituacaoAfastamento.Vigente);
        af.Tipo.Should().Be(TipoAfastamento.LicencaMaternidade);
        af.ContaTempo.Should().BeTrue();
        af.DomainEvents.Should().ContainSingle(e => e is AfastamentoRegistrado);
    }

    [Fact]
    public void Abrir_com_regra_de_tipo_diferente_deve_rejeitar()
    {
        var acao = () => Afastamento.Abrir(Tenant, Servidor, TipoAfastamento.LicencaPaternidade,
            new DateOnly(2026, 6, 1), null, null, RegraMaternidade());

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Maternidade_mantem_provento_integral_na_competencia()
    {
        var af = Afastamento.Abrir(Tenant, Servidor, TipoAfastamento.LicencaMaternidade,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 9, 28), null, RegraMaternidade());

        var efeito = af.EfeitoNa(Junho2026);

        efeito.AplicarAoProvento(3000m).Should().Be(3000m);
    }

    [Fact]
    public void Licenca_sem_vencimento_zera_o_provento_do_mes()
    {
        var af = Afastamento.Abrir(Tenant, Servidor, TipoAfastamento.LicencaSemVencimento,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), null, RegraSemVencimento());

        var efeito = af.EfeitoNa(Junho2026);

        efeito.AfetaFolha.Should().BeTrue();
        efeito.AplicarAoProvento(3000m).Should().Be(0m, "licenca sem vencimento suspende 100% dos proventos");
    }

    [Fact]
    public void Doenca_inss_no_mes_de_inicio_paga_15_dias_pelo_ente_e_suspende_o_resto()
    {
        // Afastamento o mes inteiro (30 dias): 15 dias pagos pelo ente, 15 suspensos (INSS).
        var af = Afastamento.Abrir(Tenant, Servidor, TipoAfastamento.DoencaInss,
            new DateOnly(2026, 6, 1), null, null, RegraDoencaInss());

        var efeito = af.EfeitoNa(Junho2026);

        // 30 dias afastado; 0 trabalhado; 15 dias x (3000/30) x 100% = 1500; resto suspenso.
        efeito.AplicarAoProvento(3000m).Should().Be(1500m);
    }

    [Fact]
    public void Afastamento_fora_da_competencia_tem_efeito_neutro()
    {
        var af = Afastamento.Abrir(Tenant, Servidor, TipoAfastamento.LicencaSemVencimento,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), null, RegraSemVencimento());

        var efeito = af.EfeitoNa(Junho2026);

        efeito.AfetaFolha.Should().BeFalse();
        efeito.AplicarAoProvento(3000m).Should().Be(3000m);
    }

    [Fact]
    public void Encerrar_grava_fim_efetivo_e_emite_evento()
    {
        var af = Afastamento.Abrir(Tenant, Servidor, TipoAfastamento.LicencaMaternidade,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 9, 28), null, RegraMaternidade());

        af.Encerrar(new DateOnly(2026, 9, 28));

        af.Situacao.Should().Be(SituacaoAfastamento.Encerrado);
        af.FimEfetivo.Should().Be(new DateOnly(2026, 9, 28));
        af.DomainEvents.Should().Contain(e => e is AfastamentoEncerrado);
    }

    [Fact]
    public void Determinismo_mesmas_entradas_mesmo_resultado()
    {
        var a1 = Afastamento.Abrir(Tenant, Servidor, TipoAfastamento.DoencaInss, new DateOnly(2026, 6, 1), null, null, RegraDoencaInss());
        var a2 = Afastamento.Abrir(Tenant, Servidor, TipoAfastamento.DoencaInss, new DateOnly(2026, 6, 1), null, null, RegraDoencaInss());

        a1.EfeitoNa(Junho2026).AplicarAoProvento(3000m)
            .Should().Be(a2.EfeitoNa(Junho2026).AplicarAoProvento(3000m));
    }
}
