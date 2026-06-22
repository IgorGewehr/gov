using FluentAssertions;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Iss;
using Tensorroot.Gov.Modules.Tributos.Domain.Nfse;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova do motor de apuração do ISS (domínio puro, determinístico) sobre NFS-e INGERIDAS do ADN:
/// classificação próprio/retido/substituição, alíquota por ATIVIDADE (item LC 116) parametrizável,
/// e consolidação no livro eletrônico (ApuracaoIss). Nenhuma alíquota é hardcoded — vem da tabela
/// municipal. NÃO emitimos NFS-e (passiva — ADR-0003).
/// </summary>
public sealed class MotorIssTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static NotaFiscalServico CriarNota(
        decimal valorServico,
        string item,
        bool retido = false,
        string chave = "NFSE-0001")
        => NotaFiscalServico.Importar(
            Tenant,
            chave,
            prestadorCnpj: "12345678000199",
            tomadorDocumento: null,
            ValorMonetario.De(valorServico),
            ValorMonetario.De(0m),
            new DateOnly(2026, 5, 10),
            Competencia.De(2026, 5),
            item,
            issRetidoNaFonte: retido);

    private static TabelaAliquotaIss CriarTabela()
    {
        var tabela = TabelaAliquotaIss.Criar(Tenant, 202601, "CTM Municipal (TODO validar-oficial)");
        // Item 1.07 a 2% (próprio); 7.02 a 3% com retenção obrigatória; 17.01 a 5% por substituição.
        tabela.DefinirItem("1.07", 2.0m);
        tabela.DefinirItem("7.02", 3.0m, retencaoObrigatoria: true);
        tabela.DefinirItem("17.01", 5.0m, substituicaoTributaria: true);
        tabela.Publicar();
        return tabela;
    }

    [Fact]
    public void Iss_proprio_usa_aliquota_da_atividade_do_item_lc116()
    {
        // Item 1.07 a 2%: serviço R$ 1.000 → ISS R$ 20,00, modalidade própria (prestador local).
        var nota = CriarNota(1_000m, "1.07");
        var tabela = CriarTabela();

        var memoria = CalculadoraIss.Apurar(nota, tabela);

        memoria.AliquotaPercentual.Should().Be(2.0m);
        memoria.Modalidade.Should().Be(ModalidadeIss.Proprio);
        memoria.IssApurado.Valor.Should().Be(20m);
    }

    [Fact]
    public void Iss_retido_na_fonte_quando_o_xml_indica_retencao()
    {
        // Item 1.07 (2%, sem retenção obrigatória), mas o XML indica retenção → retido na fonte.
        var nota = CriarNota(1_000m, "1.07", retido: true);
        var tabela = CriarTabela();

        var memoria = CalculadoraIss.Apurar(nota, tabela);

        memoria.Modalidade.Should().Be(ModalidadeIss.RetidoNaFonte);
        memoria.IssApurado.Valor.Should().Be(20m);
    }

    [Fact]
    public void Iss_retido_quando_a_lei_municipal_torna_a_retencao_obrigatoria_para_o_item()
    {
        // Item 7.02 a 3% com retenção obrigatória por lei municipal: serviço R$ 2.000 → ISS R$ 60,00.
        var nota = CriarNota(2_000m, "7.02");
        var tabela = CriarTabela();

        var memoria = CalculadoraIss.Apurar(nota, tabela);

        memoria.AliquotaPercentual.Should().Be(3.0m);
        memoria.Modalidade.Should().Be(ModalidadeIss.RetidoNaFonte);
        memoria.IssApurado.Valor.Should().Be(60m);
    }

    [Fact]
    public void Iss_por_substituicao_tributaria_quando_a_lei_municipal_prevê()
    {
        // Item 17.01 a 5% por substituição: serviço R$ 4.000 → ISS R$ 200,00 (substituição precede retenção).
        var nota = CriarNota(4_000m, "17.01", retido: true);
        var tabela = CriarTabela();

        var memoria = CalculadoraIss.Apurar(nota, tabela);

        memoria.AliquotaPercentual.Should().Be(5.0m);
        memoria.Modalidade.Should().Be(ModalidadeIss.SubstituicaoTributaria);
        memoria.IssApurado.Valor.Should().Be(200m);
    }

    [Fact]
    public void Item_sem_aliquota_na_tabela_falha_de_forma_explicita_nunca_presume()
    {
        var nota = CriarNota(1_000m, "99.99");
        var tabela = CriarTabela();

        var acao = () => CalculadoraIss.Apurar(nota, tabela);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*99.99*");
    }

    [Fact]
    public void Nota_cancelada_sai_da_base_de_apuracao()
    {
        var nota = CriarNota(1_000m, "1.07");
        nota.Cancelar();
        var tabela = CriarTabela();

        var acao = () => CalculadoraIss.Apurar(nota, tabela);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*Cancelada*");
    }

    [Fact]
    public void Livro_eletronico_consolida_proprio_retido_e_substituicao_por_competencia()
    {
        var tabela = CriarTabela();
        var competencia = Competencia.De(2026, 5);
        var apuracao = ApuracaoIss.Abrir(Tenant, Domain.Contribuintes.ContribuinteId.New(), competencia);

        // Próprio R$ 20 (1.07 R$1.000@2%); retido R$ 60 (7.02 R$2.000@3%); substituição R$ 200 (17.01 R$4.000@5%).
        apuracao.Escriturar(CalculadoraIss.Apurar(CriarNota(1_000m, "1.07", chave: "A"), tabela));
        apuracao.Escriturar(CalculadoraIss.Apurar(CriarNota(2_000m, "7.02", chave: "B"), tabela));
        apuracao.Escriturar(CalculadoraIss.Apurar(CriarNota(4_000m, "17.01", chave: "C"), tabela));
        apuracao.Encerrar();

        apuracao.QuantidadeNotas.Should().Be(3);
        apuracao.IssProprio.Valor.Should().Be(20m);
        apuracao.IssRetido.Valor.Should().Be(60m);
        apuracao.IssSubstituicao.Valor.Should().Be(200m);
        apuracao.Itens.Should().HaveCount(3);
    }

    [Fact]
    public void Tabela_nao_vigente_nao_apura()
    {
        var tabela = TabelaAliquotaIss.Criar(Tenant, 202601, "rascunho");
        tabela.DefinirItem("1.07", 2.0m);
        // NÃO publicada.
        var nota = CriarNota(1_000m, "1.07");

        var acao = () => CalculadoraIss.Apurar(nota, tabela);

        acao.Should().Throw<InvalidOperationException>().WithMessage("*não está vigente*");
    }
}
