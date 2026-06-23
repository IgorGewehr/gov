using FluentAssertions;
using Tensorroot.Gov.Modules.Tributos.Domain.Alvaras;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Cosip;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Melhoria;
using Tensorroot.Gov.Modules.Tributos.Domain.Taxas;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova dos motores de cálculo (domínio puro, determinístico) das espécies TAXAS/TLL, COSIP e
/// CONTRIBUIÇÃO DE MELHORIA, e da máquina de estados do ALVARÁ. Nenhum valor é hardcoded — todos os
/// valores/faixas vêm da configuração (lei municipal). Ver M6-DESIGN §3.2–§3.5.
/// </summary>
public sealed class MotorTaxasCosipMelhoriaTests
{
    private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    // ----------------------------------------------------------------------------------------------
    // TAXAS / TLL
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public void Taxa_valor_fixo_independe_da_quantidade_base()
    {
        var taxa = TabelaTaxa.Criar(Tenant, "TXLIXO", "Taxa de coleta de lixo (SV 19)", EspecieTaxa.Servico, ModoCalculoTaxa.ValorFixo, 2026, ValorMonetario.De(180m), "Art. X do CTM");
        taxa.Publicar();

        taxa.Calcular(quantidadeBase: 0m).Valor.Should().Be(180m);
        taxa.Calcular(quantidadeBase: 999m).Valor.Should().Be(180m, "no modo ValorFixo a quantidade-base é ignorada");
    }

    [Fact]
    public void Taxa_por_unidade_multiplica_o_valor_unitario_pela_quantidade()
    {
        // R$ 1,50 por m² fiscalizado × 200 m² = R$ 300,00 (área é ELEMENTO, não a base de imposto — SV 29).
        var taxa = TabelaTaxa.Criar(Tenant, "TXFISC", "Taxa de fiscalização", EspecieTaxa.PoderPolicia, ModoCalculoTaxa.PorUnidade, 2026, ValorMonetario.De(1.50m), "Art. Y do CTM");
        taxa.Publicar();

        taxa.Calcular(quantidadeBase: 200m).Valor.Should().Be(300m);
    }

    [Fact]
    public void Taxa_por_faixa_seleciona_a_faixa_da_quantidade()
    {
        var taxa = TabelaTaxa.Criar(Tenant, "TLL", "Taxa de Licença de Localização", EspecieTaxa.LicencaLocalizacaoFuncionamento, ModoCalculoTaxa.PorFaixa, 2026, ValorMonetario.Zero, "Art. Z do CTM");
        taxa.DefinirFaixa(0m, 50m, ValorMonetario.De(100m));
        taxa.DefinirFaixa(50.01m, 200m, ValorMonetario.De(300m));
        taxa.DefinirFaixa(200.01m, null, ValorMonetario.De(800m));
        taxa.Publicar();

        taxa.Calcular(30m).Valor.Should().Be(100m, "faixa 0–50");
        taxa.Calcular(150m).Valor.Should().Be(300m, "faixa 50,01–200");
        taxa.Calcular(5000m).Valor.Should().Be(800m, "faixa sem teto");
    }

    [Fact]
    public void Taxa_por_faixa_recusa_faixas_sobrepostas()
    {
        var taxa = TabelaTaxa.Criar(Tenant, "TLL", "TLL", EspecieTaxa.LicencaLocalizacaoFuncionamento, ModoCalculoTaxa.PorFaixa, 2026, ValorMonetario.Zero, "CTM");
        taxa.DefinirFaixa(0m, 100m, ValorMonetario.De(100m));

        var acao = () => taxa.DefinirFaixa(50m, 200m, ValorMonetario.De(300m));
        acao.Should().Throw<InvalidOperationException>("faixas não podem se sobrepor");
    }

    [Fact]
    public void Taxa_vigente_nao_pode_ser_alterada()
    {
        var taxa = TabelaTaxa.Criar(Tenant, "TLL", "TLL", EspecieTaxa.LicencaLocalizacaoFuncionamento, ModoCalculoTaxa.PorFaixa, 2026, ValorMonetario.Zero, "CTM");
        taxa.DefinirFaixa(0m, 100m, ValorMonetario.De(100m));
        taxa.Publicar();

        var acao = () => taxa.DefinirFaixa(100.01m, 200m, ValorMonetario.De(300m));
        acao.Should().Throw<InvalidOperationException>("tabela vigente é imutável");
    }

    [Fact]
    public void Taxa_por_faixa_sem_faixas_nao_publica()
    {
        var taxa = TabelaTaxa.Criar(Tenant, "TLL", "TLL", EspecieTaxa.LicencaLocalizacaoFuncionamento, ModoCalculoTaxa.PorFaixa, 2026, ValorMonetario.Zero, "CTM");
        var acao = () => taxa.Publicar();
        acao.Should().Throw<InvalidOperationException>();
    }

    // ----------------------------------------------------------------------------------------------
    // ALVARÁ (ato de polícia — máquina de estados)
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public void Alvara_emite_ativo_renova_e_cancela()
    {
        var alvara = Alvara.Emitir(Tenant, ContribuinteId.New(), null, EspecieAlvara.LocalizacaoFuncionamento, "Padaria do Centro", "1091-1/02", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        alvara.Situacao.Should().Be(SituacaoAlvara.Ativo);

        alvara.Renovar(new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31));
        alvara.FimVigencia.Should().Be(new DateOnly(2027, 12, 31));
        alvara.Situacao.Should().Be(SituacaoAlvara.Ativo);

        alvara.Cancelar();
        alvara.Situacao.Should().Be(SituacaoAlvara.Cancelado);

        var acaoRenovar = () => alvara.Renovar(new DateOnly(2028, 1, 1), new DateOnly(2028, 12, 31));
        acaoRenovar.Should().Throw<InvalidOperationException>("alvará cancelado não renova");
    }

    [Fact]
    public void Alvara_vence_apos_o_fim_da_vigencia()
    {
        var alvara = Alvara.Emitir(Tenant, ContribuinteId.New(), new ImovelId(Guid.NewGuid()), EspecieAlvara.Sanitario, "Restaurante", "5611-2/01", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        alvara.Vencer(new DateOnly(2027, 1, 2));
        alvara.Situacao.Should().Be(SituacaoAlvara.Vencido);
    }

    // ----------------------------------------------------------------------------------------------
    // COSIP
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public void Cosip_apura_por_faixa_de_consumo_e_classe()
    {
        var tabela = TabelaCosip.Criar(Tenant, 2026, "Lei Municipal de COSIP nº X");
        tabela.DefinirFaixa(ClasseConsumidorCosip.Residencial, 0m, 100m, ValorMonetario.De(5m));
        tabela.DefinirFaixa(ClasseConsumidorCosip.Residencial, 100.01m, 300m, ValorMonetario.De(12m));
        tabela.DefinirFaixa(ClasseConsumidorCosip.Comercial, 0m, null, ValorMonetario.De(40m));
        tabela.Publicar();

        tabela.Apurar(ClasseConsumidorCosip.Residencial, 80m).Valor.Should().Be(5m);
        tabela.Apurar(ClasseConsumidorCosip.Residencial, 250m).Valor.Should().Be(12m, "progressividade — RE 573.675");
        tabela.Apurar(ClasseConsumidorCosip.Comercial, 5000m).Valor.Should().Be(40m);
    }

    [Fact]
    public void Cosip_sem_faixa_para_o_consumo_falha()
    {
        var tabela = TabelaCosip.Criar(Tenant, 2026, "Lei COSIP");
        tabela.DefinirFaixa(ClasseConsumidorCosip.Residencial, 0m, 100m, ValorMonetario.De(5m));
        tabela.Publicar();

        var acao = () => tabela.Apurar(ClasseConsumidorCosip.Residencial, 500m);
        acao.Should().Throw<InvalidOperationException>("consumo acima de qualquer faixa configurada");
    }

    [Fact]
    public void Cosip_faixas_da_mesma_classe_nao_se_sobrepoem()
    {
        var tabela = TabelaCosip.Criar(Tenant, 2026, "Lei COSIP");
        tabela.DefinirFaixa(ClasseConsumidorCosip.Residencial, 0m, 100m, ValorMonetario.De(5m));

        var acao = () => tabela.DefinirFaixa(ClasseConsumidorCosip.Residencial, 50m, 200m, ValorMonetario.De(12m));
        acao.Should().Throw<InvalidOperationException>();
    }

    // ----------------------------------------------------------------------------------------------
    // CONTRIBUIÇÃO DE MELHORIA
    // ----------------------------------------------------------------------------------------------

    [Fact]
    public void Melhoria_exige_prazo_de_impugnacao_de_no_minimo_30_dias()
    {
        // 29 dias → rejeitado (CTN art. 82, II).
        var acao = () => PublicarObra(custo: 100_000m, financiar: 100m, publicacao: new DateOnly(2026, 1, 1), fimPrazo: new DateOnly(2026, 1, 30));
        acao.Should().Throw<ArgumentException>("prazo de impugnação inferior a 30 dias");

        // 30 dias exatos → aceito.
        var obra = PublicarObra(custo: 100_000m, financiar: 100m, publicacao: new DateOnly(2026, 1, 1), fimPrazo: new DateOnly(2026, 1, 31));
        obra.Estado.Should().Be(EstadoObraMelhoria.EditalPublicado);
    }

    [Fact]
    public void Melhoria_rateia_proporcional_a_valorizacao_respeitando_limites()
    {
        // Custo 100.000, financiar 100% → limite total 100.000. Valorizações: A=60.000, B=40.000 (soma 100.000).
        // Como soma == limite total, rateio = valorização de cada um.
        var obra = PublicarObra(custo: 100_000m, financiar: 100m, publicacao: new DateOnly(2026, 1, 1), fimPrazo: new DateOnly(2026, 2, 1));
        var imovelA = new ImovelId(Guid.NewGuid());
        var imovelB = new ImovelId(Guid.NewGuid());
        obra.AdicionarImovelBeneficiado(imovelA, ContribuinteId.New(), ValorMonetario.De(60_000m));
        obra.AdicionarImovelBeneficiado(imovelB, ContribuinteId.New(), ValorMonetario.De(40_000m));

        obra.EncerrarPrazoImpugnacao(new DateOnly(2026, 2, 1));
        var total = obra.Ratear();

        obra.Estado.Should().Be(EstadoObraMelhoria.Rateada);
        total.Valor.Should().Be(100_000m);
        obra.Imoveis.Single(i => i.ImovelId == imovelA).ContribuicaoRateada.Valor.Should().Be(60_000m);
        obra.Imoveis.Single(i => i.ImovelId == imovelB).ContribuicaoRateada.Valor.Should().Be(40_000m);
    }

    [Fact]
    public void Melhoria_limite_total_e_o_custo_financiavel_quando_menor_que_a_soma_das_valorizacoes()
    {
        // Custo 100.000, financiar 50% → limite total 50.000. Valorizações somam 100.000 (A=60k, B=40k).
        // Rateio proporcional ao TOTAL a ratear (50.000): A = 50.000×0,6 = 30.000; B = 20.000.
        var obra = PublicarObra(custo: 100_000m, financiar: 50m, publicacao: new DateOnly(2026, 1, 1), fimPrazo: new DateOnly(2026, 2, 1));
        var imovelA = new ImovelId(Guid.NewGuid());
        var imovelB = new ImovelId(Guid.NewGuid());
        obra.AdicionarImovelBeneficiado(imovelA, ContribuinteId.New(), ValorMonetario.De(60_000m));
        obra.AdicionarImovelBeneficiado(imovelB, ContribuinteId.New(), ValorMonetario.De(40_000m));

        obra.EncerrarPrazoImpugnacao(new DateOnly(2026, 2, 1));
        var total = obra.Ratear();

        total.Valor.Should().Be(50_000m, "limite total = custo × 50% (CTN art. 81)");
        obra.Imoveis.Single(i => i.ImovelId == imovelA).ContribuicaoRateada.Valor.Should().Be(30_000m);
        obra.Imoveis.Single(i => i.ImovelId == imovelB).ContribuicaoRateada.Valor.Should().Be(20_000m);
    }

    [Fact]
    public void Melhoria_nao_rateia_antes_de_encerrar_a_impugnacao()
    {
        var obra = PublicarObra(custo: 100_000m, financiar: 100m, publicacao: new DateOnly(2026, 1, 1), fimPrazo: new DateOnly(2026, 2, 1));
        obra.AdicionarImovelBeneficiado(new ImovelId(Guid.NewGuid()), ContribuinteId.New(), ValorMonetario.De(50_000m));

        var acao = () => obra.Ratear();
        acao.Should().Throw<InvalidOperationException>("rateio só após o prazo de impugnação");
    }

    [Fact]
    public void Melhoria_nao_encerra_impugnacao_antes_do_prazo()
    {
        var obra = PublicarObra(custo: 100_000m, financiar: 100m, publicacao: new DateOnly(2026, 1, 1), fimPrazo: new DateOnly(2026, 2, 1));
        var acao = () => obra.EncerrarPrazoImpugnacao(new DateOnly(2026, 1, 15));
        acao.Should().Throw<InvalidOperationException>("o prazo de impugnação ainda não decorreu");
    }

    private static ObraContribuicaoMelhoria PublicarObra(decimal custo, decimal financiar, DateOnly publicacao, DateOnly fimPrazo)
        => ObraContribuicaoMelhoria.PublicarEdital(
            Tenant,
            "Pavimentação Rua das Acácias",
            "Memorial descritivo do projeto de pavimentação.",
            ValorMonetario.De(custo),
            financiar,
            "Zona Sul - Bairro Industrial",
            fatorAbsorcaoPercentual: 100m,
            publicacao,
            fimPrazo,
            "Lei específica da obra nº X + CTM");
}
