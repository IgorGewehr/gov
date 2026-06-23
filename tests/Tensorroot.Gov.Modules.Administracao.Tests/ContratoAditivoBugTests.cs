using FluentAssertions;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Modules.Administracao.Domain.Events;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Administracao.Tests;

/// <summary>
/// Cobertura de borda dos bugs do agregado <see cref="Contrato"/> (auditoria TCE):
/// BUG-A1 (percentual derivado do valorDelta), BUG-A2 (tetos separados acrescimo/supressao),
/// BUG-A5 (supressao que zere/negative rejeitada), BUG-A6 (aditivo de prazo) e BUG-A7 (encerramento).
/// </summary>
public sealed class ContratoAditivoBugTests : AdministracaoTestBase
{
    private static Contrato ContratoEficaz(decimal valor = 100000m)
    {
        var contrato = Contrato.Celebrar(
            TenantA,
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrigemContratacao.Licitacao,
            "Servicos de limpeza",
            ValorMonetario.De(valor),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31));
        contrato.PublicarContratoPncp("PNCP-CT-0001");
        contrato.ConfirmarDotacao(EmpenhoRef.De(Guid.NewGuid(), "2026NE000001"));
        return contrato;
    }

    // ---------- BUG-A1: percentual derivado do valorDelta (fonte unica) ----------

    [Fact] // BUG-A1: valorDelta de 80% sobre o valor original estoura o teto, mesmo "parecendo" pequeno.
    public void BugA1_aditivo_com_valorDelta_acima_do_teto_e_rejeitado()
    {
        var contrato = ContratoEficaz(100000m);

        // Antes: passava informando percentual=10 solto. Agora o percentual e derivado de 80.000/100.000 = 80%.
        var acao = () => contrato.CelebrarAditivo(
            TipoAditivo.Acrescimo, ValorMonetario.De(80000m), null, "Sobrepreco mascarado", new DateOnly(2026, 6, 1));

        acao.Should().Throw<InvalidOperationException>();
        contrato.ValorAtual.Valor.Should().Be(100000m);
        contrato.PercentualQuantitativoAcumulado.Should().Be(0m);
    }

    [Fact] // BUG-A1: o percentual do evento/auditoria reflete o valorDelta REAL, nao um numero informado solto.
    public void BugA1_percentual_acumulado_reflete_o_valorDelta_real()
    {
        var contrato = ContratoEficaz(100000m);

        contrato.CelebrarAditivo(
            TipoAditivo.Acrescimo, ValorMonetario.De(20000m), null, "Acrescimo", new DateOnly(2026, 6, 1));

        contrato.PercentualAcrescimoAcumulado.Should().Be(20m);
        contrato.ValorAtual.Valor.Should().Be(120000m);
        contrato.DomainEvents.OfType<AditivoCelebrado>().Should().ContainSingle()
            .Which.PercentualAcumulado.Should().Be(20m);
    }

    [Fact] // BUG-A1: o limite acumulado e exato no valorDelta — 25% exato passa, 25,01% estoura.
    public void BugA1_fronteira_do_teto_e_exata_no_valorDelta()
    {
        var contrato = ContratoEficaz(100000m);

        contrato.CelebrarAditivo(
            TipoAditivo.Acrescimo, ValorMonetario.De(25000m), null, "No limite", new DateOnly(2026, 6, 1));
        contrato.PercentualAcrescimoAcumulado.Should().Be(25m);

        var acao = () => contrato.CelebrarAditivo(
            TipoAditivo.Acrescimo, ValorMonetario.De(10m), null, "Estoura", new DateOnly(2026, 7, 1));
        acao.Should().Throw<InvalidOperationException>();
    }

    // ---------- BUG-A2: tetos separados de acrescimo e supressao (art. 125 §1º) ----------

    [Fact] // BUG-A2: supressao 20% + acrescimo 25% deve ser ACEITO (tetos independentes); antes lancava.
    public void BugA2_supressao_e_acrescimo_tem_tetos_separados()
    {
        var contrato = ContratoEficaz(100000m);

        contrato.CelebrarAditivo(
            TipoAditivo.Supressao, ValorMonetario.De(20000m), null, "Supressao", new DateOnly(2026, 6, 1));
        contrato.CelebrarAditivo(
            TipoAditivo.Acrescimo, ValorMonetario.De(25000m), null, "Acrescimo", new DateOnly(2026, 7, 1));

        contrato.PercentualSupressaoAcumulado.Should().Be(20m);
        contrato.PercentualAcrescimoAcumulado.Should().Be(25m);
        // 100.000 - 20.000 + 25.000 = 105.000.
        contrato.ValorAtual.Valor.Should().Be(105000m);
    }

    [Fact] // BUG-A2: supressao acumulada acima de 25% e rejeitada pelo SEU proprio teto.
    public void BugA2_supressao_acumulada_acima_de_25_e_rejeitada()
    {
        var contrato = ContratoEficaz(100000m);
        contrato.CelebrarAditivo(
            TipoAditivo.Supressao, ValorMonetario.De(20000m), null, "Primeira", new DateOnly(2026, 6, 1));

        var acao = () => contrato.CelebrarAditivo(
            TipoAditivo.Supressao, ValorMonetario.De(10000m), null, "Segunda", new DateOnly(2026, 7, 1));

        acao.Should().Throw<InvalidOperationException>();
        contrato.PercentualSupressaoAcumulado.Should().Be(20m);
    }

    [Fact] // BUG-A2: supressao isolada de 25% e aceita (teto proprio), nao consome o teto de acrescimo.
    public void BugA2_supressao_no_limite_e_aceita_sem_afetar_acrescimo()
    {
        var contrato = ContratoEficaz(100000m);

        contrato.CelebrarAditivo(
            TipoAditivo.Supressao, ValorMonetario.De(25000m), null, "Supressao", new DateOnly(2026, 6, 1));

        contrato.PercentualSupressaoAcumulado.Should().Be(25m);
        contrato.PercentualAcrescimoAcumulado.Should().Be(0m);
        contrato.ValorAtual.Valor.Should().Be(75000m);
    }

    // ---------- BUG-A5: supressao que zere/negative o valor e rejeitada ----------

    [Fact] // BUG-A5: supressao com valorDelta >= ValorAtual e rejeitada (nao clampa em zero).
    public void BugA5_supressao_que_excede_valor_atual_e_rejeitada()
    {
        var contrato = ContratoEficaz(100000m);

        // valorDelta = ValorAtual (100.000) zeraria o contrato — estado impossivel.
        var acao = () => contrato.CelebrarAditivo(
            TipoAditivo.Supressao, ValorMonetario.De(100000m), null, "Zera", new DateOnly(2026, 6, 1));

        acao.Should().Throw<InvalidOperationException>();
        contrato.ValorAtual.Valor.Should().Be(100000m);
    }

    [Fact] // BUG-A5: ValorMonetario.Subtrair lanca em underflow (nao clampa silenciosamente).
    public void BugA5_subtrair_monetario_lanca_em_underflow()
    {
        var setenta = ValorMonetario.De(70000m);
        var noventa = ValorMonetario.De(90000m);

        ((Action)(() => setenta.Subtrair(noventa))).Should().Throw<InvalidOperationException>();
    }

    // ---------- BUG-A6: aditivo de prazo ----------

    [Fact] // BUG-A6: aditivo de Prazo sem nova data e rejeitado.
    public void BugA6_aditivo_prazo_exige_nova_data()
    {
        var contrato = ContratoEficaz();

        var acao = () => contrato.CelebrarAditivo(
            TipoAditivo.Prazo, ValorMonetario.Zero, null, "Sem data", new DateOnly(2026, 6, 1));

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // BUG-A6: aditivo de Prazo com data anterior/igual a vigencia atual e rejeitado (nao "fantasma").
    public void BugA6_aditivo_prazo_com_data_anterior_e_rejeitado()
    {
        var contrato = ContratoEficaz(); // vigencia ate 2026-12-31.

        var acao = () => contrato.CelebrarAditivo(
            TipoAditivo.Prazo, ValorMonetario.Zero, new DateOnly(2026, 6, 1), "Encurta", new DateOnly(2026, 6, 1));

        acao.Should().Throw<InvalidOperationException>();
        contrato.VigenciaFim.Should().Be(new DateOnly(2026, 12, 31));
    }

    [Fact] // BUG-A6: aditivo de Prazo com data posterior prorroga a vigencia.
    public void BugA6_aditivo_prazo_valido_prorroga_vigencia()
    {
        var contrato = ContratoEficaz();

        contrato.CelebrarAditivo(
            TipoAditivo.Prazo, ValorMonetario.Zero, new DateOnly(2027, 6, 30), "Prorroga", new DateOnly(2026, 6, 1));

        contrato.VigenciaFim.Should().Be(new DateOnly(2027, 6, 30));
    }

    // ---------- BUG-A7: encerramento normal vs antecipado ----------

    [Fact] // BUG-A7: encerrar contrato apenas Eficaz (sem execucao) e rejeitado — use Rescindir.
    public void BugA7_encerrar_de_Eficaz_sem_execucao_e_rejeitado()
    {
        var contrato = ContratoEficaz();

        var acao = () => contrato.Encerrar(new DateOnly(2027, 1, 1));

        acao.Should().Throw<InvalidOperationException>();
        contrato.Situacao.Should().Be(SituacaoContrato.Eficaz);
    }

    [Fact] // BUG-A7: encerramento antes do fim da vigencia e marcado como antecipado no evento.
    public void BugA7_encerramento_antecipado_e_sinalizado()
    {
        var contrato = ContratoEficaz();
        contrato.IniciarExecucao();

        contrato.Encerrar(new DateOnly(2026, 6, 1)); // antes de 2026-12-31.

        contrato.DomainEvents.OfType<ContratoEncerrado>().Should().ContainSingle()
            .Which.Antecipado.Should().BeTrue();
    }

    [Fact] // BUG-A7: encerramento apos o fim da vigencia e normal (nao antecipado).
    public void BugA7_encerramento_normal_nao_e_antecipado()
    {
        var contrato = ContratoEficaz();
        contrato.IniciarExecucao();

        contrato.Encerrar(new DateOnly(2027, 1, 1)); // apos 2026-12-31.

        contrato.DomainEvents.OfType<ContratoEncerrado>().Should().ContainSingle()
            .Which.Antecipado.Should().BeFalse();
    }
}
