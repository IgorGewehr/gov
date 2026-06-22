using FluentAssertions;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova do motor de cálculo do IPTU (domínio puro, determinístico): valor venal e imposto batendo
/// em casos conhecidos, progressividade por faixa, isenção/desconto e parametrização da PGV.
/// Nenhum valor é hardcoded no motor — todos vêm da PGV/tabela de alíquotas (lei municipal).
/// </summary>
public sealed class MotorIptuTests
{
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly ContribuinteId Proprietario = ContribuinteId.New();

    private static Imovel CriarImovel(decimal areaTerreno, decimal areaConstruida, string padrao = "MEDIO", TipoUsoImovel uso = TipoUsoImovel.Residencial)
        => Imovel.Cadastrar(
            Tenant,
            Proprietario,
            IdentificacaoImovel.Criar("000.001.0001"),
            EnderecoImovel.Criar("Rua das Flores", "Centro", "01-01-001", "ZONA-A"),
            CaracteristicasImovel.Criar(areaTerreno, areaConstruida, uso, padrao));

    private static PlantaValores CriarPgvVigente(decimal vut, decimal vuc, decimal fatorPadrao = 1m)
    {
        var pgv = PlantaValores.Criar(Tenant, 2026, "Lei Municipal PGV (TODO validar-oficial)");
        pgv.DefinirZona("ZONA-A", vut, vuc);
        if (fatorPadrao != 1m)
        {
            pgv.DefinirFator(TipoFatorPgv.PadraoConstrutivo, "MEDIO", fatorPadrao);
        }

        pgv.Publicar();
        return pgv;
    }

    private static TabelaAliquotaIptu CriarAliquotaUnica(decimal aliquota, bool edificado = true)
    {
        var tabela = TabelaAliquotaIptu.Criar(Tenant, 2026, edificado, "Lei Municipal Alíquotas (TODO validar-oficial)");
        tabela.AdicionarFaixa(0m, TabelaAliquotaIptu.SemTeto, aliquota);
        tabela.Publicar();
        return tabela;
    }

    [Fact]
    public void Valor_venal_bate_em_caso_conhecido_terreno_mais_construcao()
    {
        // Terreno 300 m² × R$ 200/m² = 60.000; Construção 120 m² × R$ 1.000/m² × fator 1 = 120.000.
        var imovel = CriarImovel(areaTerreno: 300m, areaConstruida: 120m);
        var pgv = CriarPgvVigente(vut: 200m, vuc: 1000m);

        var memoria = CalculadoraValorVenal.Calcular(imovel, pgv);

        memoria.ValorTerreno.Should().Be(60_000m);
        memoria.ValorConstrucao.Should().Be(120_000m);
        memoria.ValorVenal.Valor.Should().Be(180_000m);
    }

    [Fact]
    public void Iptu_bate_valor_venal_vezes_aliquota()
    {
        // Valor venal 180.000 × alíquota 1% = R$ 1.800,00.
        var imovel = CriarImovel(areaTerreno: 300m, areaConstruida: 120m);
        var pgv = CriarPgvVigente(vut: 200m, vuc: 1000m);
        var tabela = CriarAliquotaUnica(1.0m);

        var memoria = CalculadoraIptu.Calcular(imovel, pgv, tabela);

        memoria.ValorVenal.ValorVenal.Valor.Should().Be(180_000m);
        memoria.AliquotaPercentual.Should().Be(1.0m);
        memoria.ImpostoBruto.Valor.Should().Be(1_800m);
        memoria.ImpostoDevido.Valor.Should().Be(1_800m);
    }

    [Fact]
    public void Fator_de_padrao_construtivo_da_pgv_altera_o_valor_venal()
    {
        // O mesmo imóvel com fator de padrão 1.20 sobe a parcela de construção em 20%.
        var imovel = CriarImovel(areaTerreno: 300m, areaConstruida: 120m, padrao: "MEDIO");
        var pgv = CriarPgvVigente(vut: 200m, vuc: 1000m, fatorPadrao: 1.20m);

        var memoria = CalculadoraValorVenal.Calcular(imovel, pgv);

        memoria.FatorPadrao.Should().Be(1.20m);
        memoria.ValorConstrucao.Should().Be(144_000m); // 120 × 1000 × 1.20
        memoria.ValorVenal.Valor.Should().Be(204_000m); // 60.000 + 144.000
    }

    [Fact]
    public void Isencao_e_desconto_reduzem_o_imposto_devido()
    {
        var imovel = CriarImovel(areaTerreno: 300m, areaConstruida: 120m);
        var pgv = CriarPgvVigente(vut: 200m, vuc: 1000m);
        var tabela = CriarAliquotaUnica(1.0m);

        // Imposto bruto 1.800; isenção 50% = 900; desconto 10% sobre 900 = 90; devido = 810.
        var memoria = CalculadoraIptu.Calcular(imovel, pgv, tabela, new ParametrosIsencaoIptu(PercentualIsencao: 50m, PercentualDesconto: 10m));

        memoria.ImpostoBruto.Valor.Should().Be(1_800m);
        memoria.ValorIsencao.Valor.Should().Be(900m);
        memoria.ValorDesconto.Valor.Should().Be(90m);
        memoria.ImpostoDevido.Valor.Should().Be(810m);
    }

    [Fact]
    public void Aliquota_progressiva_seleciona_a_faixa_do_valor_venal()
    {
        var tabela = TabelaAliquotaIptu.Criar(Tenant, 2026, edificado: true, "Lei Municipal Alíquotas progressivas");
        tabela.AdicionarFaixa(0m, 100_000m, 0.5m);
        tabela.AdicionarFaixa(100_000m, 300_000m, 1.0m);
        tabela.AdicionarFaixa(300_000m, TabelaAliquotaIptu.SemTeto, 1.5m);
        tabela.Publicar();

        tabela.AliquotaPara(50_000m).Should().Be(0.5m);
        tabela.AliquotaPara(180_000m).Should().Be(1.0m);
        tabela.AliquotaPara(500_000m).Should().Be(1.5m);
    }

    [Fact]
    public void Lote_territorial_usa_vut_sem_parcela_de_construcao()
    {
        // Lote sem construção: valor venal = só terreno (300 × 150 = 45.000).
        var imovel = CriarImovel(areaTerreno: 300m, areaConstruida: 0m, uso: TipoUsoImovel.Territorial);
        var pgv = CriarPgvVigente(vut: 150m, vuc: 1000m);

        var memoria = CalculadoraValorVenal.Calcular(imovel, pgv);

        imovel.Caracteristicas.Edificado.Should().BeFalse();
        memoria.ValorConstrucao.Should().Be(0m);
        memoria.ValorVenal.Valor.Should().Be(45_000m);
    }

    [Fact]
    public void Pgv_nao_vigente_nao_calcula()
    {
        var imovel = CriarImovel(300m, 120m);
        var pgv = PlantaValores.Criar(Tenant, 2026, "rascunho");
        pgv.DefinirZona("ZONA-A", 200m, 1000m);
        // NÃO publicada.

        var acao = () => CalculadoraValorVenal.Calcular(imovel, pgv);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*não está vigente*");
    }

    [Fact]
    public void Zona_fiscal_sem_valores_na_pgv_falha_de_forma_explicita()
    {
        var imovel = Imovel.Cadastrar(
            Tenant,
            Proprietario,
            IdentificacaoImovel.Criar("000.001.0002"),
            EnderecoImovel.Criar("Rua B", "Bairro", "02-02-002", "ZONA-INEXISTENTE"),
            CaracteristicasImovel.Criar(300m, 120m, TipoUsoImovel.Residencial, "MEDIO"));
        var pgv = CriarPgvVigente(vut: 200m, vuc: 1000m);

        var acao = () => CalculadoraValorVenal.Calcular(imovel, pgv);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*ZONA-INEXISTENTE*");
    }
}
