using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Votacao"/>: invariantes, cada transicao da
/// maquina de estados e os cenarios BDD de Votacao.rules.md, sobre SQLite em memoria com auditoria
/// e isolamento por tenant.
/// </summary>
public sealed class VotacaoFluxoTests : LegislativoTestBase
{
    private static readonly DateTimeOffset Quando = new(2026, 6, 21, 15, 0, 0, TimeSpan.Zero);

    private static Votacao NovaVotacao(
        TipoVotacao tipo = TipoVotacao.Simbolica,
        MaioriaExigida maioria = MaioriaExigida.Simples,
        int totalMembros = 11,
        int presentes = 9,
        int turno = 1)
        => Votacao.Iniciar(
            TenantA,
            SessaoId.New(),
            ProposicaoId.New(),
            tipo,
            maioria,
            totalMembros,
            presentes,
            turno);

    private static void RegistrarVotos(Votacao votacao, SentidoVoto sentido, int quantidade)
    {
        for (var i = 0; i < quantidade; i++)
        {
            votacao.RegistrarVoto(VotoId.New(), VereadorId.New(), sentido, Quando);
        }
    }

    // ---------- Invariantes ----------

    [Fact] // I-1 + Cenario 1: votacao nasce Aberta e emite VotacaoIniciada.
    public void Invariante_1_inicio_nasce_aberta_e_emite_evento()
    {
        var votacao = NovaVotacao();

        votacao.Situacao.Should().Be(SituacaoVotacao.Aberta);
        votacao.Resultado.Should().BeNull();
        votacao.DomainEvents.OfType<VotacaoIniciada>().Should().ContainSingle();
    }

    [Fact] // I-1/B-1: TotalMembros/Presentes nao positivos sao rejeitados.
    public void Invariante_1_membros_ou_presentes_invalidos_sao_rejeitados()
    {
        ((Action)(() => NovaVotacao(totalMembros: 0))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => NovaVotacao(presentes: 0))).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // I-2/B-8 + Cenario 8: voto fora de votacao aberta falha.
    public void Invariante_2_voto_fora_de_aberta_falha()
    {
        var votacao = NovaVotacao();
        votacao.Encerrar();

        ((Action)(() => votacao.RegistrarVoto(VotoId.New(), VereadorId.New(), SentidoVoto.Sim, Quando)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-3 + Cenario 4 + B-6: idempotencia por votoId — um voto, um VotoRegistrado.
    public void Invariante_3_idempotencia_por_voto_id()
    {
        var votacao = NovaVotacao();
        var votoId = VotoId.New();

        votacao.RegistrarVoto(votoId, VereadorId.New(), SentidoVoto.Sim, Quando);
        votacao.RegistrarVoto(votoId, VereadorId.New(), SentidoVoto.Sim, Quando);

        votacao.Votos.Should().ContainSingle();
        votacao.DomainEvents.OfType<VotoRegistrado>().Should().ContainSingle();
    }

    [Fact] // I-4 + Cenario 10: em nominal, vereador vota uma unica vez.
    public void Invariante_4_voto_duplo_do_mesmo_vereador_em_nominal_falha()
    {
        var votacao = NovaVotacao(TipoVotacao.Nominal, MaioriaExigida.Absoluta);
        var vereador = VereadorId.New();
        votacao.RegistrarVoto(VotoId.New(), vereador, SentidoVoto.Sim, Quando);

        ((Action)(() => votacao.RegistrarVoto(VotoId.New(), vereador, SentidoVoto.Nao, Quando)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-5: encerrar apura, transita para Encerrada e emite VotacaoEncerrada.
    public void Invariante_5_encerrar_apura_e_emite_evento()
    {
        var votacao = NovaVotacao(presentes: 9);
        RegistrarVotos(votacao, SentidoVoto.Sim, 5);

        var resultado = votacao.Encerrar();

        resultado.Should().Be(ResultadoVotacao.Aprovado);
        votacao.Situacao.Should().Be(SituacaoVotacao.Encerrada);
        votacao.DomainEvents.OfType<VotacaoEncerrada>().Should().ContainSingle();
    }

    [Fact] // I-6 + Cenario 2 + B-2: maioria simples — > 50% dos presentes; empate rejeita.
    public void Invariante_6_maioria_simples()
    {
        var aprovada = NovaVotacao(maioria: MaioriaExigida.Simples, presentes: 9);
        RegistrarVotos(aprovada, SentidoVoto.Sim, 5);
        aprovada.Encerrar().Should().Be(ResultadoVotacao.Aprovado);

        // B-2: empate (votosSim == presentes/2 com presentes par) rejeita.
        var empate = NovaVotacao(maioria: MaioriaExigida.Simples, presentes: 10);
        RegistrarVotos(empate, SentidoVoto.Sim, 5);
        empate.Encerrar().Should().Be(ResultadoVotacao.Rejeitado);
    }

    [Fact] // Cenario 3: maioria simples rejeitada (4 Sim em 9 presentes).
    public void Invariante_6_maioria_simples_rejeitada()
    {
        var votacao = NovaVotacao(maioria: MaioriaExigida.Simples, presentes: 9);
        RegistrarVotos(votacao, SentidoVoto.Sim, 4);
        RegistrarVotos(votacao, SentidoVoto.Nao, 5);

        votacao.Encerrar().Should().Be(ResultadoVotacao.Rejeitado);
    }

    [Fact] // I-7 + Cenario 5/6 + B-3: maioria absoluta — >= membros/2 + 1.
    public void Invariante_7_maioria_absoluta()
    {
        var aprovada = NovaVotacao(TipoVotacao.Nominal, MaioriaExigida.Absoluta, totalMembros: 11, presentes: 11);
        RegistrarVotos(aprovada, SentidoVoto.Sim, 6);
        aprovada.Encerrar().Should().Be(ResultadoVotacao.Aprovado);

        // B-3: 5 Sim (== 11/2 sem o +1) rejeita.
        var rejeitada = NovaVotacao(TipoVotacao.Nominal, MaioriaExigida.Absoluta, totalMembros: 11, presentes: 11);
        RegistrarVotos(rejeitada, SentidoVoto.Sim, 5);
        rejeitada.Encerrar().Should().Be(ResultadoVotacao.Rejeitado);
    }

    [Fact] // I-8 + Cenario 7 + B-4: maioria qualificada — >= ceil(2*membros/3).
    public void Invariante_8_maioria_qualificada()
    {
        // ceil(2*11/3) = 8.
        var aprovada = NovaVotacao(TipoVotacao.Nominal, MaioriaExigida.Qualificada, totalMembros: 11, presentes: 11);
        RegistrarVotos(aprovada, SentidoVoto.Sim, 8);
        aprovada.Encerrar().Should().Be(ResultadoVotacao.Aprovado);

        // B-4: 7 Sim (ceil - 1) rejeita.
        var rejeitada = NovaVotacao(TipoVotacao.Nominal, MaioriaExigida.Qualificada, totalMembros: 11, presentes: 11);
        RegistrarVotos(rejeitada, SentidoVoto.Sim, 7);
        rejeitada.Encerrar().Should().Be(ResultadoVotacao.Rejeitado);
    }

    [Fact] // I-9: Resultado e nulo enquanto Aberta; preenchido no encerramento.
    public void Invariante_9_resultado_nulo_enquanto_aberta()
    {
        var votacao = NovaVotacao(presentes: 9);
        RegistrarVotos(votacao, SentidoVoto.Sim, 5);

        votacao.Resultado.Should().BeNull();
        votacao.Encerrar();
        votacao.Resultado.Should().Be(ResultadoVotacao.Aprovado);
    }

    [Fact] // I-10/B-10 + Cenario 9: encerrar votacao ja encerrada falha.
    public void Invariante_10_encerrar_ja_encerrada_falha()
    {
        var votacao = NovaVotacao();
        votacao.Encerrar();

        ((Action)(() => votacao.Encerrar())).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-13/B-5 + B-9: abstencoes nao contam como Sim; encerrar sem Sim rejeita.
    public void Invariante_13_abstencoes_nao_contam_como_sim()
    {
        var votacao = NovaVotacao(maioria: MaioriaExigida.Simples, presentes: 9);
        RegistrarVotos(votacao, SentidoVoto.Abstencao, 9);

        votacao.Abstencoes.Should().Be(9);
        votacao.Encerrar().Should().Be(ResultadoVotacao.Rejeitado);
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Cenario 13: Aberta --Cancelar--> Cancelada, sem Resultado.
    public void Transicao_cancelar_de_aberta()
    {
        var votacao = NovaVotacao();

        votacao.Cancelar();

        votacao.Situacao.Should().Be(SituacaoVotacao.Cancelada);
        votacao.Resultado.Should().BeNull();
    }

    [Fact] // B-10: cancelar votacao ja encerrada falha.
    public void Transicao_cancelar_ja_encerrada_falha()
    {
        var votacao = NovaVotacao();
        votacao.Encerrar();

        ((Action)(() => votacao.Cancelar())).Should().Throw<InvalidOperationException>();
    }

    [Fact] // Cenario 8/B-8: registrar voto em votacao cancelada falha.
    public void Transicao_voto_em_cancelada_falha()
    {
        var votacao = NovaVotacao();
        votacao.Cancelar();

        ((Action)(() => votacao.RegistrarVoto(VotoId.New(), VereadorId.New(), SentidoVoto.Sim, Quando)))
            .Should().Throw<InvalidOperationException>();
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 12: placar/votos persistem, mantem trilha imutavel e registra auditoria.
    public async Task Cenario_12_votacao_persiste_com_votos_e_auditoria()
    {
        VotacaoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var votacao = NovaVotacao(TipoVotacao.Nominal, MaioriaExigida.Absoluta, totalMembros: 11, presentes: 11);
            votacao.RegistrarVoto(VotoId.New(), VereadorId.New(), SentidoVoto.Sim, Quando);
            votacao.RegistrarVoto(VotoId.New(), VereadorId.New(), SentidoVoto.Nao, Quando);
            id = votacao.Id;
            contexto.Votacoes.Add(votacao);
            await contexto.SaveChangesAsync();

            votacao.Encerrar();
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var votacao = await contexto.Votacoes.Include(v => v.Votos).SingleAsync(v => v.Id == id);
            votacao.Situacao.Should().Be(SituacaoVotacao.Encerrada);
            votacao.Votos.Should().HaveCount(2);
            votacao.TenantId.Should().Be(TenantA);

            (await contexto.AuditTrail.ToListAsync()).Should().NotBeEmpty();
        }
    }

    [Fact] // Cenario 14/B-14: ObterVotacaoPorId e tenant-scoped (Global Query Filter).
    public async Task Cenario_14_isolamento_entre_tenants()
    {
        VotacaoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var votacao = NovaVotacao();
            id = votacao.Id;
            contexto.Votacoes.Add(votacao);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Votacoes.FirstOrDefaultAsync(v => v.Id == id)).Should().BeNull();
            (await contexto.Votacoes.ToListAsync()).Should().BeEmpty();
        }
    }
}
