using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Sessao"/>: invariantes, cada transicao da
/// maquina de estados e os cenarios BDD de Sessao.rules.md, sobre SQLite em memoria com auditoria
/// e isolamento por tenant.
/// </summary>
public sealed class SessaoFluxoTests : LegislativoTestBase
{
    private static readonly DateTimeOffset Quando = new(2026, 6, 21, 14, 0, 0, TimeSpan.Zero);

    private static Sessao NovaSessao(int totalMembros = 11, TipoSessao tipo = TipoSessao.Ordinaria)
        => Sessao.Agendar(TenantA, tipo, DataHora.De(Quando), totalMembros);

    private static void RegistrarPresencas(Sessao sessao, int quantidade)
    {
        for (var i = 0; i < quantidade; i++)
        {
            sessao.RegistrarPresenca(VereadorId.New(), Quando);
        }
    }

    // ---------- Invariantes ----------

    [Fact] // I-1: agendamento nasce em Agendada com dados validos.
    public void Invariante_1_agendamento_nasce_agendada()
    {
        var sessao = NovaSessao();

        sessao.Situacao.Should().Be(SituacaoSessao.Agendada);
        sessao.TotalMembros.Should().Be(11);
    }

    [Fact] // I-1/B-1: TotalMembros <= 0 e rejeitado.
    public void Invariante_1_total_membros_invalido_e_rejeitado()
    {
        var acao = () => Sessao.Agendar(TenantA, TipoSessao.Ordinaria, DataHora.De(Quando), 0);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-1: DataHora default e rejeitada.
    public void Invariante_1_data_hora_default_e_rejeitada()
    {
        var acao = () => DataHora.De(default);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-2 + B-2: QuorumInstalacao = maioria absoluta = TotalMembros / 2 + 1.
    public void Invariante_2_quorum_e_maioria_absoluta()
    {
        NovaSessao(11).QuorumInstalacao.Should().Be(6);
        NovaSessao(10).QuorumInstalacao.Should().Be(6);
        NovaSessao(9).QuorumInstalacao.Should().Be(5);
    }

    [Fact] // I-3 + Cenario 4 + B-10: presenca e idempotente por VereadorId.
    public void Invariante_3_presenca_e_idempotente()
    {
        var sessao = NovaSessao();
        var vereador = VereadorId.New();

        sessao.RegistrarPresenca(vereador, Quando);
        sessao.RegistrarPresenca(vereador, Quando);

        sessao.Presencas.Should().ContainSingle();
    }

    [Fact] // I-4 + Cenario 3: VerificarQuorum apura e emite QuorumVerificado.
    public void Invariante_4_verificar_quorum_emite_evento()
    {
        var sessao = NovaSessao(11);
        RegistrarPresencas(sessao, 6);

        var atingido = sessao.VerificarQuorum();

        atingido.Should().BeTrue();
        sessao.DomainEvents.OfType<QuorumVerificado>().Should().ContainSingle()
            .Which.Atingido.Should().BeTrue();
    }

    [Fact] // I-5 + Cenario 2 + B-4: abrir sem quorum falha e mantem Agendada.
    public void Invariante_5_abrir_sem_quorum_falha()
    {
        var sessao = NovaSessao(11);
        RegistrarPresencas(sessao, 5);

        ((Action)sessao.Abrir).Should().Throw<InvalidOperationException>();
        sessao.Situacao.Should().Be(SituacaoSessao.Agendada);
    }

    [Fact] // I-6 + Cenario 5: inclusao em Ordem do Dia com sessao nao terminal pauta a proposicao.
    public void Invariante_6_incluir_na_ordem_do_dia()
    {
        var sessao = NovaSessao();

        sessao.IncluirNaOrdemDoDia(ProposicaoId.New());

        sessao.OrdemDoDia.Should().ContainSingle().Which.Ordem.Should().Be(1);
    }

    [Fact] // I-7/B-6: suspender exige Aberta.
    public void Invariante_7_suspender_exige_aberta()
    {
        var sessao = NovaSessao();

        ((Action)sessao.Suspender).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-7/B-7: reabrir exige Suspensa.
    public void Invariante_7_reabrir_exige_suspensa()
    {
        var sessao = NovaSessao(11);
        RegistrarPresencas(sessao, 6);
        sessao.Abrir();

        ((Action)sessao.Reabrir).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-8/B-9: encerrar exige Aberta ou Suspensa.
    public void Invariante_8_encerrar_exige_aberta_ou_suspensa()
    {
        var sessao = NovaSessao();

        ((Action)sessao.Encerrar).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-9/B-8: cancelar exige Agendada.
    public void Invariante_9_cancelar_exige_agendada()
    {
        var sessao = NovaSessao(11);
        RegistrarPresencas(sessao, 6);
        sessao.Abrir();

        ((Action)sessao.Cancelar).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-10 + Cenario 9: sessao terminal nao admite novas operacoes nem presencas.
    public void Invariante_10_terminal_nao_admite_operacoes()
    {
        var sessao = NovaSessao(11);
        RegistrarPresencas(sessao, 6);
        sessao.Abrir();
        sessao.Encerrar();

        sessao.Terminal.Should().BeTrue();
        ((Action)(() => sessao.RegistrarPresenca(VereadorId.New(), Quando))).Should().Throw<InvalidOperationException>();
        ((Action)(() => sessao.IncluirNaOrdemDoDia(ProposicaoId.New()))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-11/B-11: incluir na Ordem do Dia em sessao terminal falha.
    public void Invariante_11_incluir_ordem_em_terminal_falha()
    {
        var sessao = NovaSessao();
        sessao.Cancelar();

        ((Action)(() => sessao.IncluirNaOrdemDoDia(ProposicaoId.New()))).Should().Throw<InvalidOperationException>();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Cenario 1 + B-3: Agendada --Abrir--> Aberta (quorum exato), emite SessaoAberta.
    public void Transicao_abrir_de_agendada_para_aberta()
    {
        var sessao = NovaSessao(11);
        RegistrarPresencas(sessao, 6);

        sessao.Abrir();

        sessao.Situacao.Should().Be(SituacaoSessao.Aberta);
        sessao.DomainEvents.OfType<SessaoAberta>().Should().ContainSingle();
    }

    [Fact] // Cenario 6: Aberta --Suspender--> Suspensa --Reabrir--> Aberta.
    public void Transicao_suspender_e_reabrir()
    {
        var sessao = NovaSessao(11);
        RegistrarPresencas(sessao, 6);
        sessao.Abrir();

        sessao.Suspender();
        sessao.Situacao.Should().Be(SituacaoSessao.Suspensa);

        sessao.Reabrir();
        sessao.Situacao.Should().Be(SituacaoSessao.Aberta);
    }

    [Fact] // Cenario 7: Aberta --Encerrar--> Encerrada, emite SessaoEncerrada.
    public void Transicao_encerrar_de_aberta()
    {
        var sessao = NovaSessao(11);
        RegistrarPresencas(sessao, 6);
        sessao.Abrir();

        sessao.Encerrar();

        sessao.Situacao.Should().Be(SituacaoSessao.Encerrada);
        sessao.DomainEvents.OfType<SessaoEncerrada>().Should().ContainSingle();
    }

    [Fact] // Suspensa --Encerrar--> Encerrada.
    public void Transicao_encerrar_de_suspensa()
    {
        var sessao = NovaSessao(11);
        RegistrarPresencas(sessao, 6);
        sessao.Abrir();
        sessao.Suspender();

        sessao.Encerrar();

        sessao.Situacao.Should().Be(SituacaoSessao.Encerrada);
    }

    [Fact] // Cenario 8: Agendada --Cancelar--> Cancelada.
    public void Transicao_cancelar_de_agendada()
    {
        var sessao = NovaSessao();

        sessao.Cancelar();

        sessao.Situacao.Should().Be(SituacaoSessao.Cancelada);
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 10: sessao com presencas persiste, mantem trilha imutavel e registra auditoria.
    public async Task Cenario_10_sessao_persiste_com_presencas_e_auditoria()
    {
        SessaoId id;
        var vereadorA = VereadorId.New();
        var vereadorB = VereadorId.New();
        await using (var contexto = CriarContexto(TenantA))
        {
            var sessao = NovaSessao(11);
            sessao.RegistrarPresenca(vereadorA, Quando);
            sessao.RegistrarPresenca(vereadorB, Quando);
            id = sessao.Id;
            contexto.Sessoes.Add(sessao);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var sessao = await contexto.Sessoes.Include(s => s.Presencas).SingleAsync(s => s.Id == id);
            sessao.Presencas.Should().HaveCount(2);
            sessao.TenantId.Should().Be(TenantA);

            (await contexto.AuditTrail.ToListAsync()).Should().NotBeEmpty();
        }
    }

    [Fact] // Cenario 11/B-13: ListarSessoesAgendadas e tenant-scoped (Global Query Filter).
    public async Task Cenario_11_isolamento_entre_tenants()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Sessoes.Add(NovaSessao());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Sessoes.ToListAsync()).Should().BeEmpty();
        }
    }
}
