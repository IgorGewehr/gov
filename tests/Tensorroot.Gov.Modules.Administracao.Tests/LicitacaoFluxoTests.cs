using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Administracao.Domain.Events;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Administracao.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Licitacao"/>: invariantes, cada transicao da
/// maquina de estados e os cenarios BDD de Licitacao.rules.md, sobre SQLite em memoria com
/// auditoria, Outbox e isolamento por tenant (Lei 14.133/2021).
/// </summary>
public sealed class LicitacaoFluxoTests : AdministracaoTestBase
{
    private static readonly DateTimeOffset Verificacao = new(2026, 6, 21, 12, 0, 0, TimeSpan.Zero);

    private static Licitacao NovoPregao()
        => Licitacao.Abrir(
            TenantA,
            "Aquisicao de notebooks",
            ModalidadeLicitacao.Pregao,
            CriterioJulgamento.MenorPreco,
            ValorMonetario.De(200000m),
            Guid.NewGuid(),
            Guid.NewGuid());

    // ---------- Invariantes ----------

    [Fact] // I-1 + I-4 + Cenario 1: abertura nasce Aberta e emite LicitacaoAberta.
    public void Invariante_4_abertura_nasce_Aberta_e_emite_evento()
    {
        var lic = NovoPregao();

        lic.Situacao.Should().Be(SituacaoLicitacao.Aberta);
        lic.ValorEstimado.Valor.Should().Be(200000m);
        lic.DomainEvents.OfType<LicitacaoAberta>().Should().ContainSingle()
            .Which.Modalidade.Should().Be(ModalidadeLicitacao.Pregao);
    }

    [Fact] // B-1: objeto vazio e rejeitado.
    public void Invariante_1_objeto_vazio_e_rejeitado()
    {
        var acao = () => Licitacao.Abrir(
            TenantA, "  ", ModalidadeLicitacao.Pregao, CriterioJulgamento.MenorPreco, ValorMonetario.De(1m));

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // B-3: modalidade fora do enum e rejeitada.
    public void Invariante_3_modalidade_invalida_e_rejeitada()
    {
        var acao = () => Licitacao.Abrir(
            TenantA, "Objeto", (ModalidadeLicitacao)99, CriterioJulgamento.MenorPreco, ValorMonetario.De(1m));

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // I-3: criterio fora do enum e rejeitado.
    public void Invariante_3_criterio_invalido_e_rejeitado()
    {
        var acao = () => Licitacao.Abrir(
            TenantA, "Objeto", ModalidadeLicitacao.Concorrencia, (CriterioJulgamento)99, ValorMonetario.De(1m));

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory] // I-5 + Cenario 2 + B-4: Pregao so admite MenorPreco/MaiorDesconto.
    [InlineData(CriterioJulgamento.TecnicaEPreco)]
    [InlineData(CriterioJulgamento.MelhorTecnica)]
    [InlineData(CriterioJulgamento.MaiorLance)]
    [InlineData(CriterioJulgamento.MaiorRetornoEconomico)]
    public void Invariante_5_pregao_com_criterio_incompativel_e_rejeitado(CriterioJulgamento criterio)
    {
        var acao = () => Licitacao.Abrir(
            TenantA, "Objeto", ModalidadeLicitacao.Pregao, criterio, ValorMonetario.De(1m));

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-5: Pregao com MaiorDesconto e aceito.
    public void Invariante_5_pregao_com_maior_desconto_e_aceito()
    {
        var lic = Licitacao.Abrir(
            TenantA, "Objeto", ModalidadeLicitacao.Pregao, CriterioJulgamento.MaiorDesconto, ValorMonetario.De(1m));

        lic.Situacao.Should().Be(SituacaoLicitacao.Aberta);
    }

    [Fact] // I-6 + B-5: julgar com proposta inexistente no certame falha.
    public void Invariante_6_julgar_proposta_inexistente_falha()
    {
        var lic = NovoPregao();

        ((Action)(() => lic.JulgarPropostas(Guid.NewGuid()))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-6: julgar so e permitido com certame Aberto.
    public void Invariante_6_julgar_exige_certame_aberto()
    {
        var lic = NovoPregao();
        var loteId = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        var propId = lic.RegistrarProposta(Guid.NewGuid(), loteId, ValorMonetario.De(90000m));
        lic.JulgarPropostas(propId.Value);

        // ja EmJulgamento -> novo julgamento falha.
        ((Action)(() => lic.JulgarPropostas(propId.Value))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-8 + Cenario 4: homologar com vencedor inabilitado falha.
    public void Invariante_8_homologar_sem_vencedor_habilitado_falha()
    {
        var lic = NovoPregao();
        var loteId = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        var fornecedor = Guid.NewGuid();
        var propId = lic.RegistrarProposta(fornecedor, loteId, ValorMonetario.De(90000m));
        lic.JulgarPropostas(propId.Value);
        lic.HabilitarLicitante(fornecedor, ResultadoHabilitacao.Inabilitado, "Doc vencido", Verificacao);

        ((Action)lic.Homologar).Should().Throw<InvalidOperationException>();
        lic.Situacao.Should().Be(SituacaoLicitacao.EmJulgamento);
    }

    [Fact] // I-8 + B-6: homologar sem vencedor definido falha (certame nem em julgamento).
    public void Invariante_8_homologar_exige_em_julgamento()
    {
        var lic = NovoPregao();

        ((Action)lic.Homologar).Should().Throw<InvalidOperationException>();
    }

    [Fact] // B-7 + Cenario 6: declarar deserta com proposta recebida falha.
    public void Invariante_11_deserta_com_proposta_falha()
    {
        var lic = NovoPregao();
        var loteId = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        lic.RegistrarProposta(Guid.NewGuid(), loteId, ValorMonetario.De(90000m));

        ((Action)lic.DeclararDeserta).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-11 + Cenario 7: fracassada exige ausencia de proposta valida/habilitada.
    public void Invariante_11_fracassada_com_proposta_valida_falha()
    {
        var lic = NovoPregao();
        var loteId = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        lic.RegistrarProposta(Guid.NewGuid(), loteId, ValorMonetario.De(90000m));

        ((Action)(() => lic.DeclararFracassada("Sem habilitados"))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-15 + Cenario 8: revogacao exige motivacao.
    public void Invariante_15_revogacao_exige_motivacao()
    {
        var lic = NovoPregao();

        ((Action)(() => lic.Revogar("  "))).Should().Throw<ArgumentException>();
    }

    [Fact] // I-15: anulacao exige motivacao.
    public void Invariante_15_anulacao_exige_motivacao()
    {
        var lic = NovoPregao();

        ((Action)(() => lic.Anular("  "))).Should().Throw<ArgumentException>();
    }

    [Fact] // I-13: publicar no PNCP exige numero do edital.
    public void Invariante_13_publicacao_pncp_exige_numero()
    {
        var lic = NovoPregao();

        ((Action)(() => lic.PublicarEditalPncp("  "))).Should().Throw<ArgumentException>();
    }

    [Fact] // Lotes so podem ser adicionados com certame Aberto.
    public void Adicionar_lote_exige_certame_aberto()
    {
        var lic = NovoPregao();
        lic.Revogar("Conveniencia");

        ((Action)(() => lic.AdicionarLote(1, "Lote", ValorMonetario.De(1m)))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-12 + Cenario 9: certame encerrado nao admite novas transicoes.
    public void Invariante_12_certame_encerrado_nao_admite_transicoes()
    {
        var lic = NovoPregao();
        lic.Revogar("Conveniencia");

        lic.Situacao.Should().Be(SituacaoLicitacao.Revogada);
        ((Action)(() => lic.Revogar("De novo"))).Should().Throw<InvalidOperationException>();
        ((Action)(() => lic.Anular("Ilegal"))).Should().Throw<InvalidOperationException>();
        ((Action)(() => lic.DeclararFracassada("X"))).Should().Throw<InvalidOperationException>();
        ((Action)lic.DeclararDeserta).Should().Throw<InvalidOperationException>();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Aberta --Julgar--> EmJulgamento; vencedora marcada.
    public void Transicao_julgar_de_Aberta_para_EmJulgamento()
    {
        var lic = NovoPregao();
        var loteId = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        var propId = lic.RegistrarProposta(Guid.NewGuid(), loteId, ValorMonetario.De(90000m));

        lic.JulgarPropostas(propId.Value);

        lic.Situacao.Should().Be(SituacaoLicitacao.EmJulgamento);
        lic.PropostaVencedoraId.Should().Be(propId.Value);
        lic.Propostas.Single(p => p.Id == propId).Situacao.Should().Be(SituacaoProposta.Vencedora);
    }

    [Fact] // EmJulgamento --Homologar--> Homologada, emite LicitacaoHomologada (Cenario 3).
    public void Transicao_homologar_de_EmJulgamento_para_Homologada()
    {
        var lic = NovoPregao();
        var loteId = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        var fornecedor = Guid.NewGuid();
        var propId = lic.RegistrarProposta(fornecedor, loteId, ValorMonetario.De(90000m));
        lic.JulgarPropostas(propId.Value);
        lic.HabilitarLicitante(fornecedor, ResultadoHabilitacao.Habilitado, null, Verificacao);

        lic.Homologar();

        lic.Situacao.Should().Be(SituacaoLicitacao.Homologada);
        var evento = lic.DomainEvents.OfType<LicitacaoHomologada>().Should().ContainSingle().Which;
        evento.FornecedorVencedorId.Should().Be(fornecedor);
        evento.PropostaVencedoraId.Should().Be(propId.Value);
        lic.FornecedorVencedorId().Should().Be(fornecedor);
        lic.ValorAdjudicado().Should().Be(90000m);
    }

    [Fact] // Aberta --DeclararDeserta--> Deserta, emite LicitacaoDeserta (Cenario 6).
    public void Transicao_deserta_de_Aberta_para_Deserta()
    {
        var lic = NovoPregao();

        lic.DeclararDeserta();

        lic.Situacao.Should().Be(SituacaoLicitacao.Deserta);
        lic.DomainEvents.OfType<LicitacaoDeserta>().Should().ContainSingle();
    }

    [Fact] // Aberta --DeclararFracassada--> Fracassada (Cenario 7): todas inabilitadas.
    public void Transicao_fracassada_de_Aberta_para_Fracassada()
    {
        var lic = NovoPregao();
        var loteId = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        var fornecedor = Guid.NewGuid();
        lic.RegistrarProposta(fornecedor, loteId, ValorMonetario.De(90000m));
        lic.HabilitarLicitante(fornecedor, ResultadoHabilitacao.Inabilitado, "Doc", Verificacao);

        lic.DeclararFracassada("Sem habilitados");

        lic.Situacao.Should().Be(SituacaoLicitacao.Fracassada);
        lic.DomainEvents.OfType<LicitacaoFracassada>().Should().ContainSingle()
            .Which.Motivo.Should().Be("Sem habilitados");
    }

    [Fact] // Aberta --Revogar--> Revogada, emite LicitacaoRevogada.
    public void Transicao_revogar_de_Aberta_para_Revogada()
    {
        var lic = NovoPregao();

        lic.Revogar("Conveniencia administrativa");

        lic.Situacao.Should().Be(SituacaoLicitacao.Revogada);
        lic.DomainEvents.OfType<LicitacaoRevogada>().Should().ContainSingle();
    }

    [Fact] // Aberta --Anular--> Anulada, emite LicitacaoAnulada.
    public void Transicao_anular_de_Aberta_para_Anulada()
    {
        var lic = NovoPregao();

        lic.Anular("Vicio de legalidade");

        lic.Situacao.Should().Be(SituacaoLicitacao.Anulada);
        lic.DomainEvents.OfType<LicitacaoAnulada>().Should().ContainSingle();
    }

    [Fact] // Cenario 10: publicacao no PNCP grava numero e emite EditalPublicadoNoPncp.
    public void Transicao_publicar_edital_pncp_emite_evento()
    {
        var lic = NovoPregao();

        lic.PublicarEditalPncp("PNCP-2026-0001");

        lic.NumeroEditalPncp.Should().Be("PNCP-2026-0001");
        lic.Situacao.Should().Be(SituacaoLicitacao.Aberta);
        lic.DomainEvents.OfType<EditalPublicadoNoPncp>().Should().ContainSingle();
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Homologacao valida persiste e registra auditoria no mesmo commit.
    public async Task Fluxo_de_homologacao_persiste_com_auditoria()
    {
        LicitacaoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var lic = NovoPregao();
            var loteId = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
            var fornecedor = Guid.NewGuid();
            var propId = lic.RegistrarProposta(fornecedor, loteId, ValorMonetario.De(90000m));
            lic.JulgarPropostas(propId.Value);
            lic.HabilitarLicitante(fornecedor, ResultadoHabilitacao.Habilitado, null, Verificacao);
            id = lic.Id;
            contexto.Licitacoes.Add(lic);
            await contexto.SaveChangesAsync();

            lic.Homologar();
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var lic = await contexto.Licitacoes.SingleAsync(l => l.Id == id);
            lic.Situacao.Should().Be(SituacaoLicitacao.Homologada);
            lic.TenantId.Should().Be(TenantA);

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes da licitacao");
        }
    }

    [Fact] // Cenario 11: consulta e tenant-scoped (Global Query Filter).
    public async Task Cenario_11_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Licitacoes.Add(NovoPregao());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Licitacoes.ToListAsync()).Should().BeEmpty();
        }
    }
}
