using FluentAssertions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Xunit;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Testes-chave do dominio de CONSIGNACOES (Onda 2 RH, design §2.7; Lei 14.131/2021): margem por balde,
/// averbacao dentro/fora da margem, baldes independentes (cartao nao invade geral), parametrizacao por
/// tenant, quitacao e cancelamento. Dominio puro (sem I/O).
/// </summary>
public sealed class ConsignacaoDominioTests
{
    private static RubricaConsignavel Rubrica(GrupoMargem grupo, CategoriaConsignavel categoria = CategoriaConsignavel.Facultativa)
        => RubricaConsignavel.Definir(Guid.NewGuid(), "9001", "Emprestimo consignado", categoria, grupo, contaParaMargem: false);

    [Fact]
    public void Margem_calcula_limite_por_balde_a_partir_dos_percentuais_legais()
    {
        // Base 1000 → 35% geral, 5% cartao consignado, 5% cartao beneficio.
        var margem = MargemConsignavel.Calcular(1000m, PercentuaisMargem.PadraoLegal);

        margem.Limite(GrupoMargem.Geral).Should().Be(350m);
        margem.Limite(GrupoMargem.CartaoConsignado).Should().Be(50m);
        margem.Limite(GrupoMargem.CartaoBeneficio).Should().Be(50m);
        margem.Disponivel(GrupoMargem.Geral).Should().Be(350m);
    }

    [Fact]
    public void Averbar_dentro_da_margem_aceita_e_nasce_averbada()
    {
        var margem = MargemConsignavel.Calcular(1000m, PercentuaisMargem.PadraoLegal);

        var contrato = ContratoConsignacao.Averbar(
            Guid.NewGuid(), ServidorId.New(), ConsignatariaId.New(), consignatariaAtiva: true,
            Rubrica(GrupoMargem.Geral), numeroContratoExterno: "C-1", valorParcela: 300m,
            quantidadeParcelas: 12, dataAverbacao: new DateOnly(2026, 6, 1), margem);

        contrato.Situacao.Should().Be(SituacaoConsignacao.Averbada);
        contrato.EstaAverbada.Should().BeTrue();
        contrato.ValorParcela.Should().Be(300m);
    }

    [Fact]
    public void Averbar_acima_da_margem_do_balde_e_rejeitada()
    {
        var margem = MargemConsignavel.Calcular(1000m, PercentuaisMargem.PadraoLegal);

        var acao = () => ContratoConsignacao.Averbar(
            Guid.NewGuid(), ServidorId.New(), ConsignatariaId.New(), consignatariaAtiva: true,
            Rubrica(GrupoMargem.Geral), numeroContratoExterno: null, valorParcela: 351m,
            quantidadeParcelas: 12, dataAverbacao: new DateOnly(2026, 6, 1), margem);

        acao.Should().Throw<ConsignacaoException>().WithMessage("*excede a margem*");
    }

    [Fact]
    public void Baldes_sao_independentes_cartao_nao_invade_geral()
    {
        // Comprometido 350 no GERAL (esgota o geral), mas o CARTAO continua com 50 livres.
        var comprometido = new Dictionary<GrupoMargem, decimal> { [GrupoMargem.Geral] = 350m };
        var margem = MargemConsignavel.Calcular(1000m, PercentuaisMargem.PadraoLegal, comprometido);

        margem.Disponivel(GrupoMargem.Geral).Should().Be(0m);
        margem.ComportaParcela(GrupoMargem.Geral, 1m).Should().BeFalse();
        margem.Disponivel(GrupoMargem.CartaoConsignado).Should().Be(50m);
        margem.ComportaParcela(GrupoMargem.CartaoConsignado, 50m).Should().BeTrue();
    }

    [Fact]
    public void Percentuais_parametrizados_por_tenant_sao_respeitados_nao_hardcoded()
    {
        // Tenant com margem geral de 40% (parametrizada, != default legal 35%).
        var percentuaisDoTenant = new PercentuaisMargem(0.40m, 0.05m, 0.05m);
        var margem = MargemConsignavel.Calcular(1000m, percentuaisDoTenant);

        margem.Limite(GrupoMargem.Geral).Should().Be(400m);
        margem.ComportaParcela(GrupoMargem.Geral, 400m).Should().BeTrue();
    }

    [Fact]
    public void Quitacao_ocorre_ao_atingir_quantidade_de_parcelas()
    {
        var margem = MargemConsignavel.Calcular(1000m, PercentuaisMargem.PadraoLegal);
        var contrato = ContratoConsignacao.Averbar(
            Guid.NewGuid(), ServidorId.New(), ConsignatariaId.New(), true,
            Rubrica(GrupoMargem.Geral), null, 100m, quantidadeParcelas: 2,
            new DateOnly(2026, 6, 1), margem);

        contrato.RegistrarParcelaPaga();
        contrato.Situacao.Should().Be(SituacaoConsignacao.Averbada);
        contrato.RegistrarParcelaPaga();
        contrato.Situacao.Should().Be(SituacaoConsignacao.Quitada);
        contrato.ParcelasPagas.Should().Be(2);

        // Apos quitada nao acumula mais parcelas (estado terminal).
        var aposQuitar = () => contrato.RegistrarParcelaPaga();
        aposQuitar.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancelamento_libera_e_impede_novo_cancelamento()
    {
        var margem = MargemConsignavel.Calcular(1000m, PercentuaisMargem.PadraoLegal);
        var contrato = ContratoConsignacao.Averbar(
            Guid.NewGuid(), ServidorId.New(), ConsignatariaId.New(), true,
            Rubrica(GrupoMargem.Geral), null, 100m, 12, new DateOnly(2026, 6, 1), margem);

        contrato.Cancelar("Quitacao antecipada pelo servidor.");
        contrato.Situacao.Should().Be(SituacaoConsignacao.Cancelada);

        var acao = () => contrato.Cancelar("de novo");
        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reativar_recheca_margem_e_falha_se_base_caiu()
    {
        var margemPlena = MargemConsignavel.Calcular(1000m, PercentuaisMargem.PadraoLegal);
        var contrato = ContratoConsignacao.Averbar(
            Guid.NewGuid(), ServidorId.New(), ConsignatariaId.New(), true,
            Rubrica(GrupoMargem.Geral), null, 300m, 12, new DateOnly(2026, 6, 1), margemPlena);
        contrato.Suspender("Pedido do servidor.");

        // Base caiu (afastamento): geral agora so 200 (35% de ~571). 300 nao cabe mais.
        var margemReduzida = MargemConsignavel.Calcular(500m, PercentuaisMargem.PadraoLegal); // geral=175
        var acao = () => contrato.Reativar(margemReduzida);

        acao.Should().Throw<ConsignacaoException>().WithMessage("*Reativacao impedida*");
    }
}
