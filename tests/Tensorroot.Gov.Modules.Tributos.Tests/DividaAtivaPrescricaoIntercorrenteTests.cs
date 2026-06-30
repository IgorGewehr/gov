using FluentAssertions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prescrição INTERCORRENTE (LEF art. 40 §4º + Súmula 314/STJ; REsp 1.340.553/RS) e SUSPENSÃO da
/// exigibilidade (CTN art. 151) na Dívida Ativa (R5 + CTN151) — [revisao-humana-juridica].
/// Datas concretas; prazo 5 anos.
/// </summary>
public sealed class DividaAtivaPrescricaoIntercorrenteTests
{
    private static readonly Guid Tenant = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static RegraEncargosDivida Regra() => RegraEncargosDivida.Criar(2m, 1m, 0.5m, "Art. 99 do CTM");

    private static DividaAtiva Inscrita(DateOnly constituicao)
        => DividaAtiva.Inscrever(
            Tenant, ContribuinteId.New(), LancamentoId.New(), TipoTributo.Iss,
            ValorMonetario.De(1_000m), constituicao, constituicao, constituicao, 1,
            "ISS — competência 03/2020", "LC 116/2003 c/c CTM", Regra());

    private static DividaAtiva EmExecucao(DateOnly constituicao, DateOnly ajuizamento)
    {
        var divida = Inscrita(constituicao);
        divida.EmitirCda("CDA-1", "Fulano de Tal", null, null, ajuizamento, null);
        divida.AjuizarExecucaoFiscal(ajuizamento);
        return divida;
    }

    // ---- R5: prescrição intercorrente (LEF art. 40) ----

    [Fact]
    public void Intercorrente_consuma_cinco_anos_apos_o_ano_de_suspensao()
    {
        var divida = EmExecucao(new DateOnly(2017, 01, 10), new DateOnly(2017, 06, 01));
        divida.SuspenderExecucao(new DateOnly(2018, 05, 15)); // ciência da não-localização

        // Suspensão até 15/05/2019; intercorrente até 15/05/2024.
        divida.EstaPrescrita(new DateOnly(2024, 05, 14)).Should().BeFalse();
        divida.EstaPrescrita(new DateOnly(2024, 05, 16)).Should().BeTrue();
    }

    [Fact]
    public void Durante_o_ano_de_suspensao_a_prescricao_nao_corre()
    {
        var divida = EmExecucao(new DateOnly(2017, 01, 10), new DateOnly(2017, 06, 01));
        divida.SuspenderExecucao(new DateOnly(2018, 05, 15));

        divida.EstaPrescrita(new DateOnly(2019, 01, 01)).Should().BeFalse();
    }

    [Fact]
    public void Constricao_ou_citacao_interrompe_o_trilho_intercorrente()
    {
        var divida = EmExecucao(new DateOnly(2017, 01, 10), new DateOnly(2017, 06, 01));
        divida.SuspenderExecucao(new DateOnly(2018, 05, 15));

        divida.RegistrarConstricaoOuCitacao(new DateOnly(2023, 03, 10)); // penhora efetiva

        divida.Situacao.Should().Be(SituacaoDividaAtiva.EmExecucaoFiscal);
        divida.EstaPrescrita(new DateOnly(2024, 05, 20)).Should().BeFalse("o trilho intercorrente foi zerado");
    }

    [Fact]
    public void Prescricao_ordinaria_inalterada_quando_nunca_ajuizada()
    {
        var divida = Inscrita(new DateOnly(2015, 01, 10));

        divida.EstaPrescrita(new DateOnly(2020, 01, 10)).Should().BeFalse("no exato dia ainda não prescreveu");
        divida.EstaPrescrita(new DateOnly(2020, 01, 11)).Should().BeTrue();
    }

    // ---- CTN 151: suspensão da exigibilidade ----

    [Fact]
    public void Deposito_integral_pausa_e_desloca_a_prescricao()
    {
        var divida = Inscrita(new DateOnly(2020, 01, 10)); // prescrição base 10/01/2025
        divida.SuspenderExigibilidade(CausaSuspensaoExigibilidade.DepositoMontanteIntegral, new DateOnly(2022, 01, 10));

        divida.EstaPrescrita(new DateOnly(2022, 06, 01)).Should().BeFalse("exigibilidade suspensa pausa a prescrição");

        divida.CessarSuspensao(new DateOnly(2023, 01, 10)); // 365 dias pausados
        divida.DataPrescricao.Should().Be(new DateOnly(2026, 01, 10));
        divida.ExigibilidadeSuspensa.Should().BeFalse();
    }

    [Fact]
    public void Liminar_em_ms_barra_a_execucao_fiscal()
    {
        var divida = Inscrita(new DateOnly(2020, 01, 10));
        divida.EmitirCda("CDA-9", "Fulano", null, null, new DateOnly(2021, 01, 01), null);
        divida.SuspenderExigibilidade(CausaSuspensaoExigibilidade.LiminarMandadoSeguranca, new DateOnly(2021, 06, 05));

        var acao = () => divida.AjuizarExecucaoFiscal(new DateOnly(2021, 06, 10));

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Nao_se_suspende_a_exigibilidade_duas_vezes()
    {
        var divida = Inscrita(new DateOnly(2020, 01, 10));
        divida.SuspenderExigibilidade(CausaSuspensaoExigibilidade.Moratoria, new DateOnly(2021, 01, 10));

        var acao = () => divida.SuspenderExigibilidade(CausaSuspensaoExigibilidade.LiminarMandadoSeguranca, new DateOnly(2021, 02, 10));

        acao.Should().Throw<InvalidOperationException>();
    }
}
