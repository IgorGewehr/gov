using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Modules.Administracao.Domain.Events;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Administracao.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Contrato"/>: invariantes, cada transicao da
/// maquina de estados (eficacia condicionada a PNCP + dotacao) e os cenarios BDD de
/// Contrato.rules.md, sobre SQLite em memoria com auditoria, Outbox e isolamento por tenant.
/// </summary>
public sealed class ContratoFluxoTests : AdministracaoTestBase
{
    private static Contrato NovoContratoLicitado(decimal valor = 100000m)
        => Contrato.Celebrar(
            TenantA,
            Guid.NewGuid(),
            Guid.NewGuid(),
            OrigemContratacao.Licitacao,
            "Servicos de limpeza",
            ValorMonetario.De(valor),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            fornecedorImpedido: false);

    private static Contrato ContratoEficaz(decimal valor = 100000m)
    {
        var contrato = NovoContratoLicitado(valor);
        contrato.PublicarContratoPncp("PNCP-CT-0001");
        contrato.ConfirmarDotacao(EmpenhoRef.De(Guid.NewGuid(), "2026NE000001"));
        return contrato;
    }

    // ---------- Invariantes ----------

    [Fact] // I-6: celebracao nasce Assinado e emite ContratoAssinado.
    public void Invariante_6_celebracao_nasce_Assinado_e_emite_evento()
    {
        var contrato = NovoContratoLicitado();

        contrato.Situacao.Should().Be(SituacaoContrato.Assinado);
        contrato.ValorContratado.Valor.Should().Be(100000m);
        contrato.ValorAtual.Valor.Should().Be(100000m);
        contrato.DomainEvents.OfType<ContratoAssinado>().Should().ContainSingle();
    }

    [Fact] // I-1: objeto vazio e rejeitado.
    public void Invariante_1_objeto_vazio_e_rejeitado()
    {
        var acao = () => Contrato.Celebrar(
            TenantA, Guid.NewGuid(), Guid.NewGuid(), OrigemContratacao.Licitacao, "  ",
            ValorMonetario.De(1m), new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), fornecedorImpedido: false);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-3: vigencia fim anterior ao inicio e rejeitada.
    public void Invariante_3_vigencia_invalida_e_rejeitada()
    {
        var acao = () => Contrato.Celebrar(
            TenantA, Guid.NewGuid(), Guid.NewGuid(), OrigemContratacao.Licitacao, "Objeto",
            ValorMonetario.De(1m), new DateOnly(2026, 5, 1), new DateOnly(2026, 1, 1), fornecedorImpedido: false);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-4: origem Licitacao exige licitacaoId.
    public void Invariante_4_origem_licitacao_exige_licitacao()
    {
        var acao = () => Contrato.Celebrar(
            TenantA, null, Guid.NewGuid(), OrigemContratacao.Licitacao, "Objeto",
            ValorMonetario.De(1m), new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), fornecedorImpedido: false);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-4 + Cenario 4: contratacao direta (Dispensa) veda licitacaoId.
    public void Invariante_4_dispensa_veda_licitacao()
    {
        var acao = () => Contrato.Celebrar(
            TenantA, Guid.NewGuid(), Guid.NewGuid(), OrigemContratacao.Dispensa, "Objeto",
            ValorMonetario.De(1m), new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), fornecedorImpedido: false);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // Cenario 4: dispensa valida sem licitacao nasce Assinado.
    public void Cenario_4_dispensa_valida_nasce_Assinado()
    {
        var contrato = Contrato.Celebrar(
            TenantA, null, Guid.NewGuid(), OrigemContratacao.Dispensa, "Compra direta",
            ValorMonetario.De(50000m), new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1), fornecedorImpedido: false);

        contrato.Situacao.Should().Be(SituacaoContrato.Assinado);
        contrato.LicitacaoId.Should().BeNull();
    }

    [Fact] // I-7: inicio da execucao exige publicacao no PNCP.
    public void Invariante_7_execucao_exige_publicacao_pncp()
    {
        var contrato = NovoContratoLicitado();
        contrato.ConfirmarDotacao(EmpenhoRef.De(Guid.NewGuid(), "2026NE000001"));

        ((Action)contrato.IniciarExecucao).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-8: inicio da execucao exige dotacao confirmada.
    public void Invariante_8_execucao_exige_dotacao()
    {
        var contrato = NovoContratoLicitado();
        contrato.PublicarContratoPncp("PNCP-CT-0001");

        ((Action)contrato.IniciarExecucao).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-9 + Cenario 6 + B: aditivo quantitativo acima de 25% e rejeitado.
    public void Invariante_9_aditivo_acima_do_limite_e_rejeitado()
    {
        var contrato = ContratoEficaz(100000m);

        var acao = () => contrato.CelebrarAditivo(
            TipoAditivo.Acrescimo, ValorMonetario.De(26000m), null, "Mais escopo", new DateOnly(2026, 6, 1));

        acao.Should().Throw<InvalidOperationException>();
        contrato.PercentualQuantitativoAcumulado.Should().Be(0m);
    }

    [Fact] // I-9 + Cenario 7: aditivo quantitativo dentro de 25% e aceito.
    public void Invariante_9_aditivo_dentro_do_limite_e_aceito()
    {
        var contrato = ContratoEficaz(100000m);

        contrato.CelebrarAditivo(
            TipoAditivo.Acrescimo, ValorMonetario.De(25000m), null, "Acrescimo", new DateOnly(2026, 6, 1));

        contrato.PercentualQuantitativoAcumulado.Should().Be(25m);
        contrato.ValorAtual.Valor.Should().Be(125000m);
        contrato.DomainEvents.OfType<AditivoCelebrado>().Should().ContainSingle();
    }

    [Fact] // I-9 + Cenario 8: reforma admite ate 50%.
    public void Invariante_9_reforma_admite_50_porcento()
    {
        var contrato = ContratoEficaz(100000m);

        contrato.CelebrarAditivo(
            TipoAditivo.Acrescimo, ValorMonetario.De(50000m), null, "Reforma", new DateOnly(2026, 6, 1), ehReforma: true);

        contrato.PercentualQuantitativoAcumulado.Should().Be(50m);
        contrato.ValorAtual.Valor.Should().Be(150000m);
    }

    [Fact] // I-9: acumulado de quantitativos cruza o limite no segundo aditivo.
    public void Invariante_9_limite_e_acumulado()
    {
        var contrato = ContratoEficaz(100000m);
        contrato.CelebrarAditivo(
            TipoAditivo.Acrescimo, ValorMonetario.De(20000m), null, "Primeiro", new DateOnly(2026, 6, 1));

        var acao = () => contrato.CelebrarAditivo(
            TipoAditivo.Acrescimo, ValorMonetario.De(10000m), null, "Segundo", new DateOnly(2026, 7, 1));

        acao.Should().Throw<InvalidOperationException>();
        contrato.PercentualQuantitativoAcumulado.Should().Be(20m);
    }

    [Fact] // I-12 + Cenario 9: garantia acima de 5% e rejeitada.
    public void Invariante_12_garantia_acima_do_limite_e_rejeitada()
    {
        var contrato = ContratoEficaz(100000m);

        var acao = () => contrato.PrestarGarantia(
            ModalidadeGarantia.SeguroGarantia, 6m, ValorMonetario.De(6000m), new DateOnly(2027, 1, 1));

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-12: grande vulto admite ate 10%.
    public void Invariante_12_grande_vulto_admite_10_porcento()
    {
        var contrato = ContratoEficaz(100000m);

        contrato.PrestarGarantia(
            ModalidadeGarantia.SeguroGarantia, 10m, ValorMonetario.De(10000m), new DateOnly(2027, 1, 1), ehGrandeVulto: true);

        contrato.Garantias.Should().ContainSingle().Which.Percentual.Should().Be(10m);
    }

    [Fact] // I-13 + Cenario 10: apostilamento dispensa aditivo e nao conta para o limite.
    public void Invariante_13_apostilamento_dispensa_aditivo()
    {
        var contrato = ContratoEficaz(100000m);

        contrato.Apostilar(TipoApostilamento.Reajuste, "Reajuste IPCA", new DateOnly(2026, 6, 1));

        contrato.Apostilamentos.Should().ContainSingle();
        contrato.Aditivos.Should().BeEmpty();
        contrato.PercentualQuantitativoAcumulado.Should().Be(0m);
    }

    [Fact] // I-15 + Cenario 12: rescisao exige motivacao.
    public void Invariante_15_rescisao_exige_motivacao()
    {
        var contrato = ContratoEficaz();

        ((Action)(() => contrato.Rescindir("  "))).Should().Throw<ArgumentException>();
    }

    [Fact] // I-14: contrato encerrado nao admite novas transicoes.
    public void Invariante_14_contrato_encerrado_nao_admite_transicoes()
    {
        var contrato = ContratoEficaz();
        contrato.IniciarExecucao();
        contrato.Encerrar(new DateOnly(2027, 1, 1));

        contrato.Situacao.Should().Be(SituacaoContrato.Encerrado);
        ((Action)(() => contrato.Rescindir("X"))).Should().Throw<InvalidOperationException>();
        ((Action)(() => contrato.Apostilar(TipoApostilamento.Correcao, "x", new DateOnly(2026, 6, 1))))
            .Should().Throw<InvalidOperationException>();
        ((Action)(() => contrato.CelebrarAditivo(
            TipoAditivo.Prazo, ValorMonetario.Zero, new DateOnly(2027, 1, 1), "x", new DateOnly(2026, 6, 1))))
            .Should().Throw<InvalidOperationException>();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Cenario 2 + Cenario 3: PNCP + dotacao -> Eficaz.
    public void Transicao_publicacao_e_dotacao_tornam_Eficaz()
    {
        var contrato = NovoContratoLicitado();
        contrato.Situacao.Should().Be(SituacaoContrato.Assinado);

        contrato.PublicarContratoPncp("PNCP-CT-0001");
        contrato.Situacao.Should().Be(SituacaoContrato.Assinado); // so PNCP nao basta (I-8).

        contrato.ConfirmarDotacao(EmpenhoRef.De(Guid.NewGuid(), "2026NE000001"));
        contrato.Situacao.Should().Be(SituacaoContrato.Eficaz);
        contrato.PublicadoNoPncp.Should().BeTrue();
        contrato.DotacaoConfirmada.Should().BeTrue();
        contrato.DomainEvents.OfType<ContratoPublicadoPncp>().Should().ContainSingle();
    }

    [Fact] // Eficaz --IniciarExecucao--> EmExecucao.
    public void Transicao_iniciar_execucao_de_Eficaz_para_EmExecucao()
    {
        var contrato = ContratoEficaz();

        contrato.IniciarExecucao();

        contrato.Situacao.Should().Be(SituacaoContrato.EmExecucao);
    }

    [Fact] // Cenario 11: dotacao indisponivel bloqueia eficacia (volta a Assinado).
    public void Cenario_11_dotacao_indisponivel_bloqueia_eficacia()
    {
        var contrato = ContratoEficaz();
        contrato.Situacao.Should().Be(SituacaoContrato.Eficaz);

        contrato.BloquearEficaciaPorDotacaoIndisponivel();

        contrato.Situacao.Should().Be(SituacaoContrato.Assinado);
        contrato.DotacaoConfirmada.Should().BeFalse();
        ((Action)contrato.IniciarExecucao).Should().Throw<InvalidOperationException>();
    }

    [Fact] // EmExecucao --Encerrar--> Encerrado, emite ContratoEncerrado (BUG-A7: exige EmExecucao).
    public void Transicao_encerrar_de_EmExecucao_para_Encerrado()
    {
        var contrato = ContratoEficaz();
        contrato.IniciarExecucao();

        contrato.Encerrar(new DateOnly(2027, 1, 1));

        contrato.Situacao.Should().Be(SituacaoContrato.Encerrado);
        contrato.DomainEvents.OfType<ContratoEncerrado>().Should().ContainSingle();
    }

    [Fact] // Cenario 12: EmExecucao --Rescindir--> Rescindido, emite ContratoRescindido.
    public void Transicao_rescindir_de_EmExecucao_para_Rescindido()
    {
        var contrato = ContratoEficaz();
        contrato.IniciarExecucao();

        contrato.Rescindir("Inexecucao contratual");

        contrato.Situacao.Should().Be(SituacaoContrato.Rescindido);
        contrato.DomainEvents.OfType<ContratoRescindido>().Should().ContainSingle()
            .Which.Motivo.Should().Be("Inexecucao contratual");
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Fluxo Eficaz->EmExecucao->Encerrado persiste com auditoria.
    public async Task Fluxo_de_execucao_persiste_com_auditoria()
    {
        ContratoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var contrato = ContratoEficaz();
            id = contrato.Id;
            contexto.Contratos.Add(contrato);
            await contexto.SaveChangesAsync();

            contrato.IniciarExecucao();
            contrato.Encerrar(new DateOnly(2027, 1, 1));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var contrato = await contexto.Contratos.SingleAsync(c => c.Id == id);
            contrato.Situacao.Should().Be(SituacaoContrato.Encerrado);
            contrato.TenantId.Should().Be(TenantA);

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes do contrato");
        }
    }

    [Fact] // Cenario 13: consulta e tenant-scoped (Global Query Filter).
    public async Task Cenario_13_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Contratos.Add(NovoContratoLicitado());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Contratos.ToListAsync()).Should().BeEmpty();
        }
    }
}
