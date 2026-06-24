using FluentAssertions;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Administracao.Tests;

/// <summary>
/// Provas dos fixes P1 do W10.6 (processo) no agregado:
/// A-1 — fornecedor com sancao impeditiva vigente NAO pode ser habilitado nem celebrar contrato
/// (fail-closed; art. 14 e art. 156, III/IV da Lei 14.133/2021);
/// A-2 — aditivo de SUPRESSAO unilateral permanece em 25% ainda que o contrato seja reforma,
/// enquanto o ACRESCIMO sobe a 50% em reforma (art. 125 caput e §1º).
/// </summary>
public sealed class W106AchadosProcessoTests : AdministracaoTestBase
{
    private static readonly DateTimeOffset Verificacao = new(2026, 6, 21, 12, 0, 0, TimeSpan.Zero);

    private static Licitacao PregaoEmJulgamentoCom(Guid fornecedor)
    {
        var lic = Licitacao.Abrir(
            TenantA,
            "Aquisicao de notebooks",
            ModalidadeLicitacao.Pregao,
            CriterioJulgamento.MenorPreco,
            ValorMonetario.De(200000m),
            Guid.NewGuid(),
            Guid.NewGuid());
        var lote = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        var prop = lic.RegistrarProposta(fornecedor, lote, ValorMonetario.De(90000m));
        lic.JulgarPropostas(prop.Value);
        return lic;
    }

    private static Contrato ContratoReformaEficaz(decimal valor = 100000m)
    {
        var contrato = Contrato.Celebrar(
            TenantA,
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrigemContratacao.Licitacao,
            "Reforma de edificio publico",
            ValorMonetario.De(valor),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            fornecedorImpedido: false,
            PrazoDivulgacaoPadrao,
            Calendario);
        contrato.PublicarContratoPncp("PNCP-CT-9001", new DateOnly(2026, 1, 2));
        contrato.ConfirmarDotacao(EmpenhoRef.De(Guid.NewGuid(), "2026NE000099"));
        return contrato;
    }

    // ---------- A-1: fornecedor impedido barrado (habilitacao + contrato) ----------

    [Fact] // A-1: habilitar como Habilitado um fornecedor impedido e ato nulo — recusado fail-closed.
    public void A1_fornecedor_impedido_nao_pode_ser_habilitado()
    {
        var fornecedor = Guid.NewGuid();
        var lic = PregaoEmJulgamentoCom(fornecedor);

        var acao = () => lic.HabilitarLicitante(
            fornecedor, ResultadoHabilitacao.Habilitado, null, Verificacao, fornecedorImpedido: true);

        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*impeditiva*");
        lic.Situacao.Should().Be(SituacaoLicitacao.EmJulgamento);
    }

    [Fact] // A-1: registrar o impedido como Inabilitado e LICITO (documenta a recusa).
    public void A1_fornecedor_impedido_pode_ser_registrado_como_inabilitado()
    {
        var fornecedor = Guid.NewGuid();
        var lic = PregaoEmJulgamentoCom(fornecedor);

        var acao = () => lic.HabilitarLicitante(
            fornecedor, ResultadoHabilitacao.Inabilitado, "Sancao impeditiva vigente", Verificacao, fornecedorImpedido: true);

        acao.Should().NotThrow();
    }

    [Fact] // A-1: homologar vencedor impedido (sancao sobreveio apos habilitacao) e recusado.
    public void A1_vencedor_impedido_nao_pode_ser_homologado()
    {
        var fornecedor = Guid.NewGuid();
        var lic = PregaoEmJulgamentoCom(fornecedor);
        lic.HabilitarLicitante(fornecedor, ResultadoHabilitacao.Habilitado, null, Verificacao, fornecedorImpedido: false);

        var acao = () => lic.Homologar(fornecedorVencedorImpedido: true);

        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*impeditiva*");
        lic.Situacao.Should().Be(SituacaoLicitacao.EmJulgamento);
    }

    [Fact] // A-1: celebrar contrato com fornecedor impedido e recusado em qualquer origem (incl. dispensa).
    public void A1_fornecedor_impedido_nao_pode_celebrar_contrato()
    {
        var acao = () => Contrato.Celebrar(
            TenantA, null, Guid.NewGuid(), OrigemContratacao.Dispensa, "Compra direta",
            ValorMonetario.De(50000m), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1),
            fornecedorImpedido: true, PrazoDivulgacaoPadrao, Calendario);

        acao.Should().Throw<InvalidOperationException>()
            .WithMessage("*impeditiva*");
    }

    // ---------- A-2: supressao em reforma limitada a 25% (acrescimo sobe a 50%) ----------

    [Fact] // A-2: em reforma, supressao unilateral acima de 25% e RECUSADA (o 50% e exclusivo dos acrescimos).
    public void A2_supressao_em_reforma_acima_de_25_e_rejeitada()
    {
        var contrato = ContratoReformaEficaz(100000m);

        // 30% de supressao "vestida" de reforma: deve ser barrada (teto da supressao = 25%).
        var acao = () => contrato.CelebrarAditivo(
            TipoAditivo.Supressao, ValorMonetario.De(30000m), null, "Supressao em reforma", new DateOnly(2026, 6, 1), ehReforma: true);

        acao.Should().Throw<InvalidOperationException>();
        contrato.PercentualSupressaoAcumulado.Should().Be(0m);
        contrato.ValorAtual.Valor.Should().Be(100000m);
    }

    [Fact] // A-2: supressao de exatamente 25% em reforma e aceita (fronteira do teto proprio).
    public void A2_supressao_em_reforma_ate_25_e_aceita()
    {
        var contrato = ContratoReformaEficaz(100000m);

        contrato.CelebrarAditivo(
            TipoAditivo.Supressao, ValorMonetario.De(25000m), null, "Supressao no limite", new DateOnly(2026, 6, 1), ehReforma: true);

        contrato.PercentualSupressaoAcumulado.Should().Be(25m);
        contrato.ValorAtual.Valor.Should().Be(75000m);
    }

    [Fact] // A-2: em reforma, o ACRESCIMO sim sobe a 50% (art. 125 §1º) — assimetria correta.
    public void A2_acrescimo_em_reforma_admite_50()
    {
        var contrato = ContratoReformaEficaz(100000m);

        contrato.CelebrarAditivo(
            TipoAditivo.Acrescimo, ValorMonetario.De(50000m), null, "Acrescimo de reforma", new DateOnly(2026, 6, 1), ehReforma: true);

        contrato.PercentualAcrescimoAcumulado.Should().Be(50m);
        contrato.ValorAtual.Valor.Should().Be(150000m);
    }

    [Fact] // A-2: acrescimo acima de 50% em reforma estoura (50% e o teto, nao piso).
    public void A2_acrescimo_em_reforma_acima_de_50_e_rejeitado()
    {
        var contrato = ContratoReformaEficaz(100000m);

        var acao = () => contrato.CelebrarAditivo(
            TipoAditivo.Acrescimo, ValorMonetario.De(51000m), null, "Acrescimo excessivo", new DateOnly(2026, 6, 1), ehReforma: true);

        acao.Should().Throw<InvalidOperationException>();
    }
}
