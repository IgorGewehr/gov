using FluentAssertions;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Iss;
using Tensorroot.Gov.Modules.Tributos.Domain.Nfse;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// W10.6 — Tributos (bordas da apuração do ISS):
/// <list type="bullet">
/// <item>T-W2: ISS é devido ao município do LOCAL da prestação (LC 116/2003 art. 3º). Nota cuja
/// incidência declarada é OUTRO município (vs. o IBGE parametrizado do tenant) não pode ser apurada
/// como ISS próprio — fail-closed. Sem o IBGE do tenant ou da nota, o confronto não se aplica (legado).</item>
/// <item>T-W1: evento de cancelamento/substituição da NFS-e (ingerido do ADN) tira a nota da base de
/// apuração; reaplicar é idempotente.</item>
/// </list>
/// </summary>
public sealed class IssIncidenciaEEventosNfseTests
{
    private static readonly Guid Tenant = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private const string IbgeTenant = "4311981"; // Maximiliano de Almeida/RS.

    private static NotaFiscalServico CriarNota(
        string item = "1.07",
        string chave = "NFSE-INC-1",
        string? municipioIncidenciaIbge = null)
        => NotaFiscalServico.Importar(
            Tenant,
            chave,
            prestadorCnpj: "12345678000199",
            tomadorDocumento: null,
            ValorMonetario.De(1_000m),
            ValorMonetario.De(0m),
            new DateOnly(2026, 5, 10),
            Competencia.De(2026, 5),
            item,
            issRetidoNaFonte: false,
            municipioIncidenciaIbge: municipioIncidenciaIbge);

    private static TabelaAliquotaIss CriarTabela(string? municipioIbge)
    {
        var tabela = TabelaAliquotaIss.Criar(Tenant, 202601, "CTM Municipal", municipioIbge);
        tabela.DefinirItem("1.07", 2.0m);
        tabela.Publicar();
        return tabela;
    }

    [Fact] // T-W2: incidência em município distinto do tenant → recusa apurar como ISS próprio.
    public void Recusa_apurar_nota_com_incidencia_em_outro_municipio()
    {
        var tabela = CriarTabela(IbgeTenant);
        var nota = CriarNota(municipioIncidenciaIbge: "4314902"); // Porto Alegre/RS.

        var acao = () => CalculadoraIss.Apurar(nota, tabela);

        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*devido a outro município*");
    }

    [Fact] // T-W2: incidência no próprio município do tenant → apura normalmente.
    public void Apura_nota_com_incidencia_no_municipio_do_tenant()
    {
        var tabela = CriarTabela(IbgeTenant);
        var nota = CriarNota(municipioIncidenciaIbge: IbgeTenant);

        var memoria = CalculadoraIss.Apurar(nota, tabela);

        memoria.IssApurado.Valor.Should().Be(20m, "1.07 a 2% sobre R$ 1.000 = R$ 20");
    }

    [Fact] // T-W2: sem IBGE do tenant OU da nota, confronto não se aplica (comportamento legado preservado).
    public void Confronto_de_municipio_nao_se_aplica_sem_codigos_ibge()
    {
        var tabelaSemIbge = CriarTabela(municipioIbge: null);
        var notaComIncidencia = CriarNota(municipioIncidenciaIbge: "4314902");
        // Tabela sem IBGE: legado — não confronta.
        CalculadoraIss.Apurar(notaComIncidencia, tabelaSemIbge).IssApurado.Valor.Should().Be(20m);

        var tabelaComIbge = CriarTabela(IbgeTenant);
        var notaSemIncidencia = CriarNota(municipioIncidenciaIbge: null);
        // Nota sem município de incidência: legado — não confronta.
        CalculadoraIss.Apurar(notaSemIncidencia, tabelaComIbge).IssApurado.Valor.Should().Be(20m);
    }

    [Fact] // T-W1: nota cancelada sai da apuração (fail-closed) e Cancelar é idempotente.
    public void Nota_cancelada_nao_entra_na_apuracao()
    {
        var tabela = CriarTabela(IbgeTenant);
        var nota = CriarNota(municipioIncidenciaIbge: IbgeTenant);

        nota.Cancelar();
        nota.Cancelar(); // idempotente

        nota.Situacao.Should().Be(SituacaoNfse.Cancelada);
        var acao = () => CalculadoraIss.Apurar(nota, tabela);
        acao.Should().Throw<InvalidOperationException>().WithMessage("*não entra na apuração*");
    }

    [Fact] // T-W1: nota substituída sai da apuração e Substituir é idempotente.
    public void Nota_substituida_nao_entra_na_apuracao()
    {
        var tabela = CriarTabela(IbgeTenant);
        var nota = CriarNota(municipioIncidenciaIbge: IbgeTenant);

        nota.Substituir();
        nota.Substituir(); // idempotente

        nota.Situacao.Should().Be(SituacaoNfse.Substituida);
        var acao = () => CalculadoraIss.Apurar(nota, tabela);
        acao.Should().Throw<InvalidOperationException>().WithMessage("*não entra na apuração*");
    }
}
