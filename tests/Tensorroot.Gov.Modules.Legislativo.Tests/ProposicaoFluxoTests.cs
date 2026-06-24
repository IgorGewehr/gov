using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Proposicao"/>: invariantes, cada transicao da
/// maquina de estados e os cenarios BDD de Proposicao.rules.md, sobre SQLite em memoria com
/// auditoria, Outbox e isolamento por tenant.
/// </summary>
public sealed class ProposicaoFluxoTests : LegislativoTestBase
{
    private static readonly DateOnly Hoje = new(2026, 6, 21);

    private static Proposicao NovaProposicao(TipoProposicao tipo = TipoProposicao.ProjetoDeLeiOrdinaria, string protocolo = "PROT-0001")
        => Proposicao.Apresentar(
            TenantA,
            tipo,
            Ementa.De("Dispoe sobre o servico publico municipal"),
            Autoria.De("Vereador Joao"),
            RegimeTramitacao.Ordinario,
            Hoje,
            protocolo);

    private static Proposicao ProposicaoEmOrdemDoDia(TipoProposicao tipo = TipoProposicao.ProjetoDeLeiOrdinaria)
    {
        var proposicao = NovaProposicao(tipo);
        proposicao.Distribuir(Hoje);
        proposicao.RegistrarParecer(Proposicao.ComissaoCcj, favoravel: true, Hoje);
        proposicao.RegistrarParecer(Proposicao.ComissaoFinancasOrcamento, favoravel: true, Hoje);
        proposicao.IncluirEmOrdemDoDia(Hoje);
        return proposicao;
    }

    // ---------- Invariantes ----------

    [Fact] // I-1 + Cenario 1: apresentacao nasce Apresentada e emite ProposicaoApresentada.
    public void Invariante_1_apresentacao_nasce_valida_e_emite_evento()
    {
        var proposicao = NovaProposicao();

        proposicao.Situacao.Should().Be(SituacaoProposicao.Apresentada);
        proposicao.DomainEvents.OfType<ProposicaoApresentada>().Should().ContainSingle();
        proposicao.Tramitacoes.Should().ContainSingle(t => t.Fase == FaseTramitacao.Apresentacao);
    }

    [Fact] // I-1/B-1: Ementa vazia e rejeitada.
    public void Invariante_1_ementa_vazia_e_rejeitada()
    {
        var acao = () => Ementa.De("   ");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-1/B-1: Autoria vazia e rejeitada.
    public void Invariante_1_autoria_vazia_e_rejeitada()
    {
        var acao = () => Autoria.De("");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact] // I-2: a maioria exigida deriva do tipo e e imutavel apos a apresentacao.
    public void Invariante_2_maioria_exigida_deriva_do_tipo()
    {
        NovaProposicao(TipoProposicao.ProjetoDeLeiOrdinaria).MaioriaExigida.Should().Be(MaioriaProposicao.Simples);
        NovaProposicao(TipoProposicao.ProjetoDeLeiComplementar).MaioriaExigida.Should().Be(MaioriaProposicao.Absoluta);
        NovaProposicao(TipoProposicao.ProjetoDeResolucao).MaioriaExigida.Should().Be(MaioriaProposicao.Absoluta);
        NovaProposicao(TipoProposicao.EmendaALOM).MaioriaExigida.Should().Be(MaioriaProposicao.Qualificada);
    }

    [Fact] // I-3/B-2: distribuir fora de Apresentada falha.
    public void Invariante_3_distribuir_fora_de_apresentada_falha()
    {
        var proposicao = NovaProposicao();
        proposicao.Distribuir(Hoje);

        ((Action)(() => proposicao.Distribuir(Hoje))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-4 + Cenario 11: emenda so e permitida em tramitacao; emite EmendaApresentada.
    public void Invariante_4_emenda_em_tramitacao_emite_evento()
    {
        var proposicao = NovaProposicao();
        proposicao.Distribuir(Hoje);

        proposicao.ApresentarEmenda("Altera o artigo 1o", Autoria.De("Vereador Ana"), Hoje);

        proposicao.Emendas.Should().ContainSingle();
        proposicao.DomainEvents.OfType<EmendaApresentada>().Should().ContainSingle();
    }

    [Fact] // I-4: substitutivo so e permitido em tramitacao; emite EmendaApresentada.
    public void Invariante_4_substitutivo_em_tramitacao_emite_evento()
    {
        var proposicao = NovaProposicao();
        proposicao.Distribuir(Hoje);

        proposicao.ApresentarSubstitutivo("Texto integral substituto", Autoria.De("Vereador Ana"), Hoje);

        proposicao.Substitutivos.Should().ContainSingle();
        proposicao.DomainEvents.OfType<EmendaApresentada>().Should().ContainSingle();
    }

    [Fact] // I-4/B-10: emenda sobre proposicao terminal falha (nao em tramitacao).
    public void Invariante_4_emenda_sobre_terminal_falha()
    {
        var proposicao = NovaProposicao();
        proposicao.Arquivar(Hoje);

        ((Action)(() => proposicao.ApresentarEmenda("X", Autoria.De("Y"), Hoje)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-5: registrar parecer em tramitacao emite ParecerEmitido na trilha imutavel.
    public void Invariante_5_registrar_parecer_emite_evento()
    {
        var proposicao = NovaProposicao();
        proposicao.Distribuir(Hoje);

        proposicao.RegistrarParecer(Proposicao.ComissaoCcj, favoravel: true, Hoje);

        proposicao.Tramitacoes.Should().Contain(t => t.Fase == FaseTramitacao.Parecer);
        proposicao.DomainEvents.OfType<ParecerEmitido>().Should().ContainSingle();
    }

    [Fact] // I-6 + Cenario 3 + B-4: sem parecer da CCJ a inclusao em Ordem do Dia e viciada.
    public void Invariante_6_inclusao_sem_parecer_ccj_e_viciada()
    {
        var proposicao = NovaProposicao(TipoProposicao.ProjetoDeLeiComplementar);
        proposicao.Distribuir(Hoje);
        proposicao.RegistrarParecer(Proposicao.ComissaoFinancasOrcamento, favoravel: true, Hoje);

        ((Action)(() => proposicao.IncluirEmOrdemDoDia(Hoje))).Should().Throw<InvalidOperationException>();
        proposicao.Situacao.Should().Be(SituacaoProposicao.Distribuida);
    }

    [Fact] // I-6 + B-3: sem parecer de Financas a inclusao e viciada.
    public void Invariante_6_inclusao_sem_parecer_financas_e_viciada()
    {
        var proposicao = NovaProposicao();
        proposicao.Distribuir(Hoje);
        proposicao.RegistrarParecer(Proposicao.ComissaoCcj, favoravel: true, Hoje);

        ((Action)(() => proposicao.IncluirEmOrdemDoDia(Hoje))).Should().Throw<InvalidOperationException>();
        proposicao.Situacao.Should().Be(SituacaoProposicao.Distribuida);
    }

    [Fact] // I-7: inclusao em Ordem do Dia exige Distribuida.
    public void Invariante_7_inclusao_exige_distribuida()
    {
        var proposicao = NovaProposicao();

        ((Action)(() => proposicao.IncluirEmOrdemDoDia(Hoje))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-8/B-5: PLO sem maioria simples atingida nao aprova.
    public void Invariante_8_aprovar_plo_sem_maioria_falha()
    {
        var proposicao = ProposicaoEmOrdemDoDia(TipoProposicao.ProjetoDeLeiOrdinaria);

        ((Action)(() => proposicao.Aprovar(ResultadoDeliberacao.Rejeitada(), turno: 1, Interstico.DeDias(1), Hoje)))
            .Should().Throw<InvalidOperationException>();
        proposicao.Situacao.Should().Be(SituacaoProposicao.EmOrdemDoDia);
    }

    [Fact] // Cenario 8/B-7: EmendaALOM sem 2/3 (maioria simples atingida) nao satisfaz a exigida.
    public void Invariante_8_aprovar_emenda_lom_com_maioria_simples_falha()
    {
        var proposicao = ProposicaoEmOrdemDoDia(TipoProposicao.EmendaALOM);

        ((Action)(() => proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Simples), turno: 1, Interstico.DeDias(1), Hoje)))
            .Should().Throw<InvalidOperationException>();
    }

    // ---------- L-1: Emenda a LOM exige DOIS turnos (CF/88 art. 29, caput) ----------

    [Fact] // L-1: a especie EmendaALOM exige 2 turnos; as demais, turno unico.
    public void L1_emenda_lom_exige_dois_turnos_demais_turno_unico()
    {
        NovaProposicao(TipoProposicao.EmendaALOM).TurnosExigidos.Should().Be(2);
        NovaProposicao(TipoProposicao.EmendaALOM).ExigeDoisTurnos.Should().BeTrue();
        NovaProposicao(TipoProposicao.ProjetoDeLeiOrdinaria).TurnosExigidos.Should().Be(1);
        NovaProposicao(TipoProposicao.ProjetoDeLeiComplementar).ExigeDoisTurnos.Should().BeFalse();
    }

    [Fact] // L-1 (NUCLEO): EmendaALOM NAO se aprova em turno unico — apos o 1o turno fica em Ordem do Dia.
    public void L1_emenda_lom_nao_aprova_em_turno_unico()
    {
        var proposicao = ProposicaoEmOrdemDoDia(TipoProposicao.EmendaALOM);

        proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Qualificada), turno: 1, Interstico.DeDias(1), Hoje);

        proposicao.Situacao.Should().Be(SituacaoProposicao.EmOrdemDoDia);
        proposicao.TurnosAprovados.Should().Be(1);
        proposicao.DomainEvents.OfType<ProposicaoAprovada>().Should().BeEmpty();
        proposicao.DomainEvents.OfType<TurnoAprovado>().Should().ContainSingle();
    }

    [Fact] // L-1: dois turnos qualificados com intersticio observado => Aprovada (e so entao ProposicaoAprovada).
    public void L1_emenda_lom_aprova_apos_dois_turnos_com_intersticio()
    {
        var proposicao = ProposicaoEmOrdemDoDia(TipoProposicao.EmendaALOM);
        var intersticio = Interstico.DeDias(1);

        proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Qualificada), turno: 1, intersticio, Hoje);
        proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Qualificada), turno: 2, intersticio, Hoje.AddDays(1));

        proposicao.Situacao.Should().Be(SituacaoProposicao.Aprovada);
        proposicao.TurnosAprovados.Should().Be(2);
        proposicao.DomainEvents.OfType<ProposicaoAprovada>().Should().ContainSingle();
    }

    [Fact] // L-1: os dois turnos no MESMO dia violam o intersticio minimo (datas/sessoes distintas).
    public void L1_emenda_lom_dois_turnos_no_mesmo_dia_viola_intersticio()
    {
        var proposicao = ProposicaoEmOrdemDoDia(TipoProposicao.EmendaALOM);
        var intersticio = Interstico.DeDias(1);
        proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Qualificada), turno: 1, intersticio, Hoje);

        ((Action)(() => proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Qualificada), turno: 2, intersticio, Hoje)))
            .Should().Throw<InvalidOperationException>();
        proposicao.Situacao.Should().Be(SituacaoProposicao.EmOrdemDoDia);
        proposicao.TurnosAprovados.Should().Be(1);
    }

    [Fact] // L-1: o 2o turno fora de sequencia (sem o 1o) e recusado.
    public void L1_segundo_turno_sem_o_primeiro_e_recusado()
    {
        var proposicao = ProposicaoEmOrdemDoDia(TipoProposicao.EmendaALOM);

        ((Action)(() => proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Qualificada), turno: 2, Interstico.DeDias(1), Hoje)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // L-1: cada turno exige a maioria qualificada; 2o turno sem 2/3 nao aprova a materia.
    public void L1_segundo_turno_sem_maioria_qualificada_nao_aprova()
    {
        var proposicao = ProposicaoEmOrdemDoDia(TipoProposicao.EmendaALOM);
        var intersticio = Interstico.DeDias(1);
        proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Qualificada), turno: 1, intersticio, Hoje);

        ((Action)(() => proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Absoluta), turno: 2, intersticio, Hoje.AddDays(1))))
            .Should().Throw<InvalidOperationException>();
        proposicao.Situacao.Should().Be(SituacaoProposicao.EmOrdemDoDia);
    }

    [Fact] // L-1: materia de turno unico nao admite turno 2.
    public void L1_materia_turno_unico_nao_admite_turno_dois()
    {
        var proposicao = ProposicaoEmOrdemDoDia(TipoProposicao.ProjetoDeLeiOrdinaria);

        ((Action)(() => proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Simples), turno: 2, Interstico.DeDias(1), Hoje)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-9 + Cenario 6: rejeicao exige EmOrdemDoDia, passa a Rejeitada e emite evento.
    public void Invariante_9_rejeitar_de_ordem_do_dia()
    {
        var proposicao = ProposicaoEmOrdemDoDia();

        proposicao.Rejeitar(Hoje);

        proposicao.Situacao.Should().Be(SituacaoProposicao.Rejeitada);
        proposicao.DomainEvents.OfType<ProposicaoRejeitada>().Should().ContainSingle();
    }

    [Fact] // I-10/B-9: gerar autografo com numero vazio e rejeitado.
    public void Invariante_10_autografo_sem_numero_e_rejeitado()
    {
        var proposicao = ProposicaoEmOrdemDoDia();
        proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Simples), turno: 1, Interstico.DeDias(1), Hoje);

        ((Action)(() => proposicao.GerarAutografo("   ", Hoje))).Should().Throw<ArgumentException>();
    }

    [Fact] // I-10/B-8: gerar autografo fora de Aprovada e rejeitado.
    public void Invariante_10_autografo_fora_de_aprovada_e_rejeitado()
    {
        var proposicao = ProposicaoEmOrdemDoDia();

        ((Action)(() => proposicao.GerarAutografo("AUT-2026-0001", Hoje))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-11/B-11: arquivar proposicao ja terminal falha.
    public void Invariante_11_arquivar_terminal_falha()
    {
        var proposicao = ProposicaoEmOrdemDoDia();
        proposicao.Rejeitar(Hoje);

        ((Action)(() => proposicao.Arquivar(Hoje))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-12: estado AutografoEnviado nao admite novas transicoes de tramitacao.
    public void Invariante_12_autografo_enviado_nao_admite_transicoes()
    {
        var proposicao = ProposicaoEmOrdemDoDia();
        proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Simples), turno: 1, Interstico.DeDias(1), Hoje);
        proposicao.GerarAutografo("AUT-2026-0001", Hoje);

        proposicao.Situacao.Should().Be(SituacaoProposicao.AutografoEnviado);
        proposicao.EmTramitacao.Should().BeFalse();
        ((Action)(() => proposicao.ApresentarEmenda("X", Autoria.De("Y"), Hoje))).Should().Throw<InvalidOperationException>();
        ((Action)(() => proposicao.RegistrarParecer(Proposicao.ComissaoCcj, true, Hoje))).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-13/B-12: sancao/veto so se aplicam a AutografoEnviado.
    public void Invariante_13_sancao_e_veto_so_em_autografo_enviado()
    {
        var emTramitacao = NovaProposicao();
        emTramitacao.RegistrarSancao(Hoje).Should().BeFalse();
        emTramitacao.RegistrarVeto(Hoje).Should().BeFalse();

        var enviado = ProposicaoEmOrdemDoDia();
        enviado.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Simples), turno: 1, Interstico.DeDias(1), Hoje);
        enviado.GerarAutografo("AUT-2026-0002", Hoje);
        enviado.RegistrarSancao(Hoje).Should().BeTrue();
    }

    [Fact] // I-14: toda mutacao de situacao registra uma Tramitacao na trilha imutavel.
    public void Invariante_14_cada_transicao_registra_tramitacao()
    {
        var proposicao = ProposicaoEmOrdemDoDia();
        proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Simples), turno: 1, Interstico.DeDias(1), Hoje);
        proposicao.GerarAutografo("AUT-2026-0003", Hoje);

        proposicao.Tramitacoes.Select(t => t.Fase).Should().Contain(new[]
        {
            FaseTramitacao.Apresentacao,
            FaseTramitacao.Distribuicao,
            FaseTramitacao.OrdemDoDia,
            FaseTramitacao.Aprovacao,
            FaseTramitacao.Autografo,
        });
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // Cenario 2: Apresentada --Distribuir--> Distribuida, emite ProposicaoDistribuida.
    public void Transicao_distribuir_de_apresentada_para_distribuida()
    {
        var proposicao = NovaProposicao();

        proposicao.Distribuir(Hoje);

        proposicao.Situacao.Should().Be(SituacaoProposicao.Distribuida);
        proposicao.DomainEvents.OfType<ProposicaoDistribuida>().Should().ContainSingle();
    }

    [Fact] // Cenario 4: Distribuida --IncluirEmOrdemDoDia--> EmOrdemDoDia (pareceres presentes).
    public void Transicao_incluir_em_ordem_do_dia_valida()
    {
        var proposicao = ProposicaoEmOrdemDoDia();

        proposicao.Situacao.Should().Be(SituacaoProposicao.EmOrdemDoDia);
    }

    [Fact] // Cenario 5: PLO em EmOrdemDoDia aprovado por maioria simples emite ProposicaoAprovada.
    public void Transicao_aprovar_plo_por_maioria_simples()
    {
        var proposicao = ProposicaoEmOrdemDoDia(TipoProposicao.ProjetoDeLeiOrdinaria);

        proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Simples), turno: 1, Interstico.DeDias(1), Hoje);

        proposicao.Situacao.Should().Be(SituacaoProposicao.Aprovada);
        proposicao.DomainEvents.OfType<ProposicaoAprovada>().Should().ContainSingle();
    }

    [Fact] // Cenario 9: PLC aprovado por maioria absoluta.
    public void Transicao_aprovar_plc_por_maioria_absoluta()
    {
        var proposicao = ProposicaoEmOrdemDoDia(TipoProposicao.ProjetoDeLeiComplementar);

        proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Absoluta), turno: 1, Interstico.DeDias(1), Hoje);

        proposicao.Situacao.Should().Be(SituacaoProposicao.Aprovada);
    }

    [Fact] // Cenario 7: Aprovada --GerarAutografo--> AutografoEnviado, emite AutografoEnviado.
    public void Transicao_gerar_autografo_de_aprovada()
    {
        var proposicao = ProposicaoEmOrdemDoDia();
        proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Simples), turno: 1, Interstico.DeDias(1), Hoje);

        proposicao.GerarAutografo("AUT-2026-0001", Hoje);

        proposicao.Situacao.Should().Be(SituacaoProposicao.AutografoEnviado);
        proposicao.NumeroAutografo.Should().Be("AUT-2026-0001");
        proposicao.DomainEvents.OfType<AutografoEnviado>().Should().ContainSingle();
    }

    [Fact] // Cenario 10: nao terminal --Arquivar--> Arquivada, emite ProposicaoArquivada.
    public void Transicao_arquivar_de_nao_terminal()
    {
        var proposicao = NovaProposicao();
        proposicao.Distribuir(Hoje);

        proposicao.Arquivar(Hoje);

        proposicao.Situacao.Should().Be(SituacaoProposicao.Arquivada);
        proposicao.DomainEvents.OfType<ProposicaoArquivada>().Should().ContainSingle();
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 14: fluxo aprovado/autografo persiste e registra auditoria.
    public async Task Cenario_14_fluxo_persiste_com_auditoria()
    {
        ProposicaoId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var proposicao = ProposicaoEmOrdemDoDia();
            id = proposicao.Id;
            contexto.Proposicoes.Add(proposicao);
            await contexto.SaveChangesAsync();

            proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Simples), turno: 1, Interstico.DeDias(1), Hoje);
            proposicao.GerarAutografo("AUT-2026-9999", Hoje);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var proposicao = await contexto.Proposicoes
                .Include(p => p.Tramitacoes)
                .SingleAsync(p => p.Id == id);
            proposicao.Situacao.Should().Be(SituacaoProposicao.AutografoEnviado);
            proposicao.TenantId.Should().Be(TenantA);
            proposicao.Tramitacoes.Should().NotBeEmpty();

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes da proposicao");
        }
    }

    [Fact] // B-13: protocolo duplicado no mesmo tenant viola o indice unico (TenantId, Protocolo).
    public async Task B_13_protocolo_duplicado_no_mesmo_tenant_e_rejeitado()
    {
        await using var contexto = CriarContexto(TenantA);
        contexto.Proposicoes.Add(NovaProposicao(protocolo: "PROT-DUP"));
        await contexto.SaveChangesAsync();

        contexto.Proposicoes.Add(NovaProposicao(protocolo: "PROT-DUP"));
        var acao = async () => await contexto.SaveChangesAsync();

        await acao.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact] // Cenario 15/B-15: consulta e tenant-scoped (Global Query Filter).
    public async Task Isolamento_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Proposicoes.Add(NovaProposicao());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Proposicoes.ToListAsync()).Should().BeEmpty();
        }
    }
}
