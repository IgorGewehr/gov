using FluentAssertions;
using Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;
using Xunit;

namespace Tensorroot.Gov.Modules.Educacao.Tests;

/// <summary>
/// Cobertura de unidade do FUNDEB (E-2 piso de 70% / E-3 distribuição): o atingimento/não do piso de 70%
/// na remuneração dos profissionais (EC 108/2020, parametrizável), a conciliação recebido × esperado por
/// origem (cota-parte/VAAF/VAAT/VAAR) e a reprodutibilidade.
/// </summary>
public sealed class FundebDomainTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const decimal SetentaPorCento = 0.70m;

    [Fact]
    public void Piso_de_70_porcento_atingido_quando_remuneracao_alcanca_a_meta()
    {
        // Receita FUNDEB 1M; remuneração 700k = 70%.
        var indicador = IndicadorAplicacaoFundeb.Apurar(1_000_000m, 700_000m, SetentaPorCento);

        indicador.PercentualAplicado.Should().Be(0.70m);
        indicador.Atingido.Should().BeTrue();
        indicador.Situacao.Should().Be(SituacaoFundeb70.Atingido);
        indicador.MargemPontos.Should().Be(0m);
    }

    [Fact]
    public void Piso_de_70_porcento_nao_atingido_quando_remuneracao_fica_abaixo()
    {
        var indicador = IndicadorAplicacaoFundeb.Apurar(1_000_000m, 650_000m, SetentaPorCento); // 65%

        indicador.PercentualAplicado.Should().Be(0.65m);
        indicador.Atingido.Should().BeFalse();
        indicador.MargemPontos.Should().BeLessThan(0m);
    }

    [Fact]
    public void Piso_e_parametrizavel_default_70_porcento_ec_108()
    {
        // Mesmos 65% atingem o antigo piso de 60% (magistério), mas NÃO o piso de 70% (EC 108/2020).
        IndicadorAplicacaoFundeb.Apurar(1_000_000m, 650_000m, 0.60m).Atingido.Should().BeTrue();
        IndicadorAplicacaoFundeb.Apurar(1_000_000m, 650_000m, 0.70m).Atingido.Should().BeFalse();
    }

    [Fact]
    public void Aplicacao_fundeb_e_reprodutivel_sem_relogio()
    {
        var primeira = IndicadorAplicacaoFundeb.Apurar(987_654m, 700_000m, SetentaPorCento);
        var segunda = IndicadorAplicacaoFundeb.Apurar(987_654m, 700_000m, SetentaPorCento);

        segunda.Should().Be(primeira);
    }

    [Fact]
    public void Distribuicao_concilia_recebido_por_origem_e_consolida_a_receita()
    {
        var distribuicao = DistribuicaoFundeb.Criar(Tenant, 2026);

        distribuicao.DefinirEsperado(OrigemRecursoFundeb.CotaParteEstadual, 800_000m);
        distribuicao.DefinirEsperado(OrigemRecursoFundeb.ComplementacaoVaat, 200_000m);

        distribuicao.ReceberParcela(OrigemRecursoFundeb.CotaParteEstadual, 500_000m);
        distribuicao.ReceberParcela(OrigemRecursoFundeb.CotaParteEstadual, 300_000m);
        distribuicao.ReceberParcela(OrigemRecursoFundeb.ComplementacaoVaat, 150_000m);

        distribuicao.RecebidoDaOrigem(OrigemRecursoFundeb.CotaParteEstadual).Should().Be(800_000m);
        distribuicao.DivergenciaDaOrigem(OrigemRecursoFundeb.CotaParteEstadual).Should().Be(0m);
        // VAAT recebeu 150k de 200k esperados → 50k a receber (divergência negativa).
        distribuicao.DivergenciaDaOrigem(OrigemRecursoFundeb.ComplementacaoVaat).Should().Be(-50_000m);
        // Receita total (base do 70%) = soma das origens recebidas.
        distribuicao.ReceitaFundebTotal.Should().Be(950_000m);
        distribuicao.EsperadoTotal.Should().Be(1_000_000m);
    }
}
