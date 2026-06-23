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

    private static Imovel CriarImovel(
        decimal areaTerreno,
        decimal areaConstruida,
        string padrao = "MEDIO",
        TipoUsoImovel uso = TipoUsoImovel.Residencial,
        int? anoConstrucao = null,
        decimal fracaoIdeal = 1m)
        => Imovel.Cadastrar(
            Tenant,
            Proprietario,
            IdentificacaoImovel.Criar("000.001.0001"),
            EnderecoImovel.Criar("Rua das Flores", "Centro", "01-01-001", "ZONA-A"),
            CaracteristicasImovel.Criar(areaTerreno, areaConstruida, uso, padrao, anoConstrucao, fracaoIdeal));

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

    // ----------------------------------------------------------------------------------------------
    // IP-B1 — Faixa "sem teto" cobre o topo (inclusiva/infinita); Publicar exige cobertura do topo.
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public void Aliquota_para_valor_venal_exatamente_no_sentinela_sem_teto_casa_a_ultima_faixa()
    {
        // IP-B1: imóvel de valor venal == SemTeto NÃO pode ficar a descoberto (não-lançamento por exceção).
        var tabela = TabelaAliquotaIptu.Criar(Tenant, 2026, edificado: true, "Lei Municipal");
        tabela.AdicionarFaixa(0m, 300_000m, 1.0m);
        tabela.AdicionarFaixa(300_000m, TabelaAliquotaIptu.SemTeto, 1.5m);
        tabela.Publicar();

        tabela.AliquotaPara(TabelaAliquotaIptu.SemTeto).Should().Be(1.5m);
    }

    [Fact]
    public void Aliquota_na_fronteira_exata_entre_faixas_pertence_a_faixa_superior()
    {
        // Fronteira [min, max): o limite superior pertence à faixa seguinte (inclusivo no min).
        var tabela = TabelaAliquotaIptu.Criar(Tenant, 2026, edificado: true, "Lei Municipal");
        tabela.AdicionarFaixa(0m, 100_000m, 0.5m);
        tabela.AdicionarFaixa(100_000m, TabelaAliquotaIptu.SemTeto, 1.0m);
        tabela.Publicar();

        tabela.AliquotaPara(99_999.99m).Should().Be(0.5m);   // logo abaixo da fronteira
        tabela.AliquotaPara(100_000m).Should().Be(1.0m);     // exatamente na fronteira → faixa superior
        tabela.AliquotaPara(100_000.01m).Should().Be(1.0m);  // logo acima
    }

    [Fact]
    public void Publicar_rejeita_tabela_cuja_ultima_faixa_nao_cobre_o_topo()
    {
        // IP-B1: última faixa com teto finito deixaria imóveis de alto valor sem alíquota → barrar.
        var tabela = TabelaAliquotaIptu.Criar(Tenant, 2026, edificado: true, "Lei Municipal");
        tabela.AdicionarFaixa(0m, 100_000m, 0.5m);
        tabela.AdicionarFaixa(100_000m, 500_000m, 1.0m); // teto finito, NÃO cobre o topo

        var acao = () => tabela.Publicar();

        acao.Should().Throw<InvalidOperationException>().WithMessage("*cobrir o topo*");
    }

    [Fact]
    public void Imovel_de_alto_valor_venal_e_lancado_em_vez_de_dar_excecao()
    {
        // Imóvel cujo valor venal alcança o sentinela é tributado normalmente (não vira renúncia silenciosa).
        var imovel = CriarImovel(areaTerreno: TabelaAliquotaIptu.SemTeto, areaConstruida: 0m, uso: TipoUsoImovel.Territorial);
        var pgv = CriarPgvVigente(vut: 1m, vuc: 1m); // valor venal = SemTeto × 1
        var tabela = TabelaAliquotaIptu.Criar(Tenant, 2026, edificado: false, "Lei Municipal");
        tabela.AdicionarFaixa(0m, TabelaAliquotaIptu.SemTeto, 0.5m);
        tabela.Publicar();

        var memoria = CalculadoraIptu.Calcular(imovel, pgv, tabela);

        memoria.ValorVenal.ValorVenal.Valor.Should().Be(TabelaAliquotaIptu.SemTeto);
        memoria.AliquotaPercentual.Should().Be(0.5m);
    }

    // ----------------------------------------------------------------------------------------------
    // IP-R4 — Depreciação pelo EXERCÍCIO do fato gerador (reprodutível) + casamento por INTERVALO.
    // ----------------------------------------------------------------------------------------------

    private static PlantaValores CriarPgvComDepreciacao(int exercicio, params (string Faixa, decimal Fator)[] faixas)
    {
        var pgv = PlantaValores.Criar(Tenant, exercicio, "Lei Municipal PGV");
        pgv.DefinirZona("ZONA-A", 200m, 1000m);
        foreach (var (faixa, fator) in faixas)
        {
            pgv.DefinirFator(TipoFatorPgv.Depreciacao, faixa, fator);
        }

        pgv.Publicar();
        return pgv;
    }

    [Fact]
    public void Depreciacao_usa_o_exercicio_do_fato_gerador_e_nao_o_relogio()
    {
        // Construída em 2016. PGV exercício 2026 → idade 10 → faixa "6-10" → fator 0.80.
        // Reprodutibilidade: o número NÃO depende do ano em que se reapura (sem DateTime.UtcNow).
        var imovel = CriarImovel(areaTerreno: 0m, areaConstruida: 100m, anoConstrucao: 2016);
        var pgv = CriarPgvComDepreciacao(2026, ("0-5", 1.00m), ("6-10", 0.80m), ("11+", 0.60m));

        var memoria = CalculadoraValorVenal.Calcular(imovel, pgv);

        memoria.FatorDepreciacao.Should().Be(0.80m);
        memoria.ValorConstrucao.Should().Be(80_000m); // 100 × 1000 × 0.80
    }

    [Fact]
    public void Depreciacao_e_reproduzivel_mesmo_imovel_e_exercicio_da_sempre_o_mesmo_valor()
    {
        var imovel = CriarImovel(areaTerreno: 0m, areaConstruida: 100m, anoConstrucao: 2016);
        var pgv = CriarPgvComDepreciacao(2026, ("0-5", 1.00m), ("6-10", 0.80m), ("11+", 0.60m));

        var primeira = CalculadoraValorVenal.Calcular(imovel, pgv);
        var segunda = CalculadoraValorVenal.Calcular(imovel, pgv);

        segunda.FatorDepreciacao.Should().Be(primeira.FatorDepreciacao);
        segunda.ValorVenal.Valor.Should().Be(primeira.ValorVenal.Valor);
    }

    [Fact]
    public void Idade_intermediaria_casa_a_faixa_por_intervalo_e_nao_cai_em_fator_neutro()
    {
        // IP-R4: idade 7 deve casar a faixa 6–10 (e não exigir chave exata "7" → fator 1 silencioso).
        var imovel = CriarImovel(areaTerreno: 0m, areaConstruida: 100m, anoConstrucao: 2019);
        var pgv = CriarPgvComDepreciacao(2026, ("0-5", 1.00m), ("6-10", 0.80m), ("11+", 0.60m));

        var memoria = CalculadoraValorVenal.Calcular(imovel, pgv);

        memoria.FatorDepreciacao.Should().Be(0.80m); // idade 7 ∈ [6,10]
    }

    [Theory]
    [InlineData(2026, 1.00)] // idade 0 → faixa 0-5
    [InlineData(2021, 1.00)] // idade 5 → fronteira superior da faixa 0-5
    [InlineData(2020, 0.80)] // idade 6 → fronteira inferior da faixa 6-10
    [InlineData(2016, 0.80)] // idade 10 → fronteira superior da faixa 6-10
    [InlineData(2015, 0.60)] // idade 11 → faixa aberta 11+
    [InlineData(1900, 0.60)] // idade 126 → faixa aberta 11+
    public void Depreciacao_no_limite_de_faixa_casa_o_intervalo_correto(int anoConstrucao, decimal fatorEsperado)
    {
        var imovel = CriarImovel(areaTerreno: 0m, areaConstruida: 100m, anoConstrucao: anoConstrucao);
        var pgv = CriarPgvComDepreciacao(2026, ("0-5", 1.00m), ("6-10", 0.80m), ("11+", 0.60m));

        var memoria = CalculadoraValorVenal.Calcular(imovel, pgv);

        memoria.FatorDepreciacao.Should().Be(fatorEsperado);
    }

    [Fact]
    public void Depreciacao_ausente_na_pgv_falha_em_vez_de_cair_em_fator_neutro_silencioso()
    {
        // IP-R4 fail-closed: PGV define faixas de depreciação, mas nenhuma cobre a idade → LANÇA.
        var imovel = CriarImovel(areaTerreno: 0m, areaConstruida: 100m, anoConstrucao: 2000); // idade 26
        var pgv = CriarPgvComDepreciacao(2026, ("0-5", 1.00m), ("6-10", 0.80m)); // sem faixa para 26

        var acao = () => CalculadoraValorVenal.Calcular(imovel, pgv);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*nenhuma cobre a idade*");
    }

    [Fact]
    public void Sem_faixas_de_depreciacao_na_pgv_o_fator_e_neutro_um()
    {
        // Município que não deprecia por idade: ausência TOTAL de faixas é legítima → fator 1.
        var imovel = CriarImovel(areaTerreno: 0m, areaConstruida: 100m, anoConstrucao: 2000);
        var pgv = CriarPgvVigente(vut: 200m, vuc: 1000m); // sem fatores de depreciação

        var memoria = CalculadoraValorVenal.Calcular(imovel, pgv);

        memoria.FatorDepreciacao.Should().Be(1m);
        memoria.ValorConstrucao.Should().Be(100_000m); // 100 × 1000 × 1
    }

    [Fact]
    public void Sem_ano_de_construcao_nao_ha_depreciacao_a_aplicar()
    {
        var imovel = CriarImovel(areaTerreno: 0m, areaConstruida: 100m, anoConstrucao: null);
        var pgv = CriarPgvComDepreciacao(2026, ("0-5", 1.00m), ("6-10", 0.80m));

        var memoria = CalculadoraValorVenal.Calcular(imovel, pgv);

        memoria.FatorDepreciacao.Should().Be(1m);
    }

    [Fact]
    public void Construcao_com_ano_futuro_em_relacao_ao_exercicio_e_tratada_como_idade_zero()
    {
        // Ano de construção > exercício (cadastro de obra futura): idade clampada a 0 → faixa 0-5.
        var imovel = CriarImovel(areaTerreno: 0m, areaConstruida: 100m, anoConstrucao: 2030);
        var pgv = CriarPgvComDepreciacao(2026, ("0-5", 1.00m), ("6-10", 0.80m));

        var memoria = CalculadoraValorVenal.Calcular(imovel, pgv);

        memoria.FatorDepreciacao.Should().Be(1.00m);
    }

    // ----------------------------------------------------------------------------------------------
    // IP-B3 — Convenção de FracaoIdeal: fração sobre o VALOR VENAL TOTAL (terreno + construção).
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public void Fracao_ideal_rateia_o_valor_venal_total_terreno_mais_construcao()
    {
        // Unidade autônoma: fração 0,02 sobre (terreno comum + construção privativa).
        // Terreno 1000 × 150 = 150.000; construção 80 × 1000 = 80.000; total 230.000 × 0,02 = 4.600.
        var imovel = CriarImovel(areaTerreno: 1000m, areaConstruida: 80m, fracaoIdeal: 0.02m);
        var pgv = CriarPgvVigente(vut: 150m, vuc: 1000m);

        var memoria = CalculadoraValorVenal.Calcular(imovel, pgv);

        memoria.FracaoIdeal.Should().Be(0.02m);
        memoria.ValorTerreno.Should().Be(150_000m);
        memoria.ValorConstrucao.Should().Be(80_000m);
        memoria.ValorVenal.Valor.Should().Be(4_600m); // (150.000 + 80.000) × 0,02
    }

    [Fact]
    public void Fracao_ideal_um_e_neutra_para_imovel_nao_condominial()
    {
        var imovel = CriarImovel(areaTerreno: 300m, areaConstruida: 120m, fracaoIdeal: 1m);
        var pgv = CriarPgvVigente(vut: 200m, vuc: 1000m);

        var memoria = CalculadoraValorVenal.Calcular(imovel, pgv);

        memoria.FracaoIdeal.Should().Be(1m);
        memoria.ValorVenal.Valor.Should().Be(180_000m);
    }

    // ----------------------------------------------------------------------------------------------
    // Isenção / desconto — fronteiras (zera, ordem, arredondamento).
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public void Isencao_de_cem_por_cento_zera_o_imposto_devido()
    {
        var imovel = CriarImovel(areaTerreno: 300m, areaConstruida: 120m);
        var pgv = CriarPgvVigente(vut: 200m, vuc: 1000m);
        var tabela = CriarAliquotaUnica(1.0m);

        var memoria = CalculadoraIptu.Calcular(imovel, pgv, tabela, new ParametrosIsencaoIptu(PercentualIsencao: 100m));

        memoria.ImpostoBruto.Valor.Should().Be(1_800m);
        memoria.ValorIsencao.Valor.Should().Be(1_800m);
        memoria.ImpostoDevido.Valor.Should().Be(0m);
    }

    [Fact]
    public void Isencao_aplica_antes_do_desconto_que_incide_sobre_o_saldo()
    {
        // bruto 1.999 → isenção 33% = 659,67 → saldo 1.339,33 → desconto 7% = 93,75 → devido 1.245,58.
        var imovel = CriarImovel(areaTerreno: 0m, areaConstruida: 199_900m);
        var pgv = CriarPgvVigente(vut: 0m, vuc: 1m); // valor venal = 199.900
        var tabela = CriarAliquotaUnica(1.0m);       // 1% de 199.900 = 1.999

        var memoria = CalculadoraIptu.Calcular(imovel, pgv, tabela, new ParametrosIsencaoIptu(PercentualIsencao: 33m, PercentualDesconto: 7m));

        memoria.ImpostoBruto.Valor.Should().Be(1_999m);
        memoria.ValorIsencao.Valor.Should().Be(659.67m);   // 1.999 × 33%
        memoria.ValorDesconto.Valor.Should().Be(93.75m);   // (1.999 − 659,67) × 7% = 1.339,33 × 7%
        memoria.ImpostoDevido.Valor.Should().Be(1_245.58m);
    }

    [Fact]
    public void Desconto_de_cota_unica_a_vista_reduz_o_imposto_devido()
    {
        var imovel = CriarImovel(areaTerreno: 300m, areaConstruida: 120m);
        var pgv = CriarPgvVigente(vut: 200m, vuc: 1000m);
        var tabela = CriarAliquotaUnica(1.0m);

        // Cota única: desconto à vista de 10% sobre o bruto (sem isenção) → 1.800 − 180 = 1.620.
        var memoria = CalculadoraIptu.Calcular(imovel, pgv, tabela, new ParametrosIsencaoIptu(PercentualDesconto: 10m));

        memoria.ValorDesconto.Valor.Should().Be(180m);
        memoria.ImpostoDevido.Valor.Should().Be(1_620m);
    }

    // ----------------------------------------------------------------------------------------------
    // Só-territorial / áreas — terreno sem construção e seleção de tabela territorial.
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public void Lote_so_territorial_nao_aplica_depreciacao_nem_fator_de_construcao()
    {
        // Sem construção (área 0): a parcela de construção é 0 independentemente de qualquer fator.
        var imovel = CriarImovel(areaTerreno: 500m, areaConstruida: 0m, uso: TipoUsoImovel.Territorial, anoConstrucao: 1990);
        var pgv = CriarPgvComDepreciacao(2026, ("0-5", 1.00m), ("6-10", 0.80m)); // não cobre idade 36

        var memoria = CalculadoraValorVenal.Calcular(imovel, pgv);

        memoria.ValorConstrucao.Should().Be(0m);
        memoria.ValorVenal.Valor.Should().Be(100_000m); // 500 × 200 (VUT da ZONA-A)
    }

    [Fact]
    public void Areas_zero_produzem_valor_venal_zero_e_imposto_zero()
    {
        // RISCO-8 documentado: imóvel sem terreno e sem construção → valor venal 0 → IPTU 0.
        var imovel = CriarImovel(areaTerreno: 0m, areaConstruida: 0m, uso: TipoUsoImovel.Territorial);
        var pgv = CriarPgvVigente(vut: 200m, vuc: 1000m);
        var tabela = CriarAliquotaUnica(1.0m, edificado: false);

        var memoria = CalculadoraIptu.Calcular(imovel, pgv, tabela);

        memoria.ValorVenal.ValorVenal.Valor.Should().Be(0m);
        memoria.ImpostoDevido.Valor.Should().Be(0m);
    }
}
