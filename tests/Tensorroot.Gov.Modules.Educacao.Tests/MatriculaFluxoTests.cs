using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;
using Xunit;

namespace Tensorroot.Gov.Modules.Educacao.Tests;

/// <summary>
/// Cobertura de integracao do agregado <see cref="Matricula"/>: invariantes, cada transicao da
/// maquina de estados (Ativa -&gt; Transferida/Concluida/Abandono) e os cenarios BDD de
/// Matricula.rules.md, sobre SQLite em memoria com auditoria, Outbox e isolamento por tenant.
/// </summary>
public sealed class MatriculaFluxoTests : EducacaoTestBase
{
    private static readonly DateOnly DataReferencia = new(2026, 3, 31);

    private static Matricula NovaMatricula()
        => Matricula.MatricularAluno(
            TenantA,
            new AlunoId(Guid.NewGuid()),
            new TurmaId(Guid.NewGuid()),
            new EscolaId(Guid.NewGuid()),
            DataReferencia);

    // ---------- Invariantes ----------

    [Fact] // I-1 + Cenario 1: matricula nasce Ativa e emite AlunoMatriculado.
    public void Invariante_1_matricula_nasce_Ativa_e_emite_evento()
    {
        var matricula = NovaMatricula();

        matricula.Situacao.Should().Be(SituacaoMatricula.Ativa);
        matricula.DataReferencia.Should().Be(DataReferencia);
        matricula.DomainEvents.OfType<AlunoMatriculado>().Should().ContainSingle();
    }

    [Fact] // I-4: a Matricula Inicial reflete a data de referencia do Censo.
    public void Invariante_4_reflete_data_de_referencia()
    {
        var matricula = NovaMatricula();

        matricula.DataReferencia.Should().Be(DataReferencia);
    }

    [Fact] // I-5 + Cenario 7 + B-5: transicao sobre matricula encerrada e rejeitada.
    public void Invariante_5_transicao_exige_Ativa()
    {
        var matricula = NovaMatricula();
        matricula.Concluir();

        ((Action)matricula.Transferir).Should().Throw<InvalidOperationException>();
        ((Action)matricula.Concluir).Should().Throw<InvalidOperationException>();
        ((Action)matricula.RegistrarAbandono).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-6 + Cenario 4: transferencia leva a Transferida e emite AlunoTransferido.
    public void Invariante_6_transferencia_leva_a_Transferida_e_emite_evento()
    {
        var matricula = NovaMatricula();

        matricula.Transferir();

        matricula.Situacao.Should().Be(SituacaoMatricula.Transferida);
        matricula.DomainEvents.OfType<AlunoTransferido>().Should().ContainSingle();
    }

    [Fact] // I-7 + Cenario 5: conclusao leva a Concluida e emite MatriculaEncerrada.
    public void Invariante_7_conclusao_leva_a_Concluida_e_emite_evento()
    {
        var matricula = NovaMatricula();

        matricula.Concluir();

        matricula.Situacao.Should().Be(SituacaoMatricula.Concluida);
        matricula.DomainEvents.OfType<MatriculaEncerrada>().Should().ContainSingle();
    }

    [Fact] // I-8 + Cenario 6: abandono leva a Abandono e emite MatriculaEncerrada.
    public void Invariante_8_abandono_leva_a_Abandono_e_emite_evento()
    {
        var matricula = NovaMatricula();

        matricula.RegistrarAbandono();

        matricula.Situacao.Should().Be(SituacaoMatricula.Abandono);
        matricula.DomainEvents.OfType<MatriculaEncerrada>().Should().ContainSingle();
    }

    [Fact] // I-9: estados terminais nao admitem novas transicoes (cobre Transferida/Concluida/Abandono).
    public void Invariante_9_estados_terminais_nao_admitem_transicoes()
    {
        var transferida = NovaMatricula();
        transferida.Transferir();
        ((Action)transferida.Concluir).Should().Throw<InvalidOperationException>();

        var concluida = NovaMatricula();
        concluida.Concluir();
        ((Action)concluida.RegistrarAbandono).Should().Throw<InvalidOperationException>();

        var abandono = NovaMatricula();
        abandono.RegistrarAbandono();
        ((Action)abandono.Transferir).Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-10 + Cenario 8: registrar Situacao do Aluno preenche o VO sem alterar a situacao.
    public void Invariante_10_registrar_situacao_do_aluno_preenche_vo()
    {
        var matricula = NovaMatricula();

        matricula.RegistrarSituacaoDoAluno(Rendimento.Aprovado, Movimento.SemMovimento);

        matricula.Situacao.Should().Be(SituacaoMatricula.Ativa);
        matricula.SituacaoDoAluno.Should().NotBeNull();
        matricula.SituacaoDoAluno!.Rendimento.Should().Be(Rendimento.Aprovado);
        matricula.SituacaoDoAluno.Movimento.Should().Be(Movimento.SemMovimento);
    }

    [Fact] // I-5/I-10: registrar Situacao do Aluno exige matricula Ativa.
    public void Invariante_5_registrar_situacao_do_aluno_exige_Ativa()
    {
        var matricula = NovaMatricula();
        matricula.Concluir();

        var acao = () => matricula.RegistrarSituacaoDoAluno(Rendimento.Aprovado, Movimento.SemMovimento);

        acao.Should().Throw<InvalidOperationException>();
    }

    // ---------- Maquina de estados (transicoes) ----------

    [Fact] // (nenhum) --MatricularAluno--> Ativa.
    public void Transicao_matricular_para_Ativa()
    {
        var matricula = NovaMatricula();

        matricula.Situacao.Should().Be(SituacaoMatricula.Ativa);
    }

    [Fact] // Ativa --Transferir--> Transferida.
    public void Transicao_transferir_de_Ativa_para_Transferida()
    {
        var matricula = NovaMatricula();

        matricula.Transferir();

        matricula.Situacao.Should().Be(SituacaoMatricula.Transferida);
    }

    [Fact] // Ativa --Concluir--> Concluida.
    public void Transicao_concluir_de_Ativa_para_Concluida()
    {
        var matricula = NovaMatricula();

        matricula.Concluir();

        matricula.Situacao.Should().Be(SituacaoMatricula.Concluida);
    }

    [Fact] // Ativa --RegistrarAbandono--> Abandono.
    public void Transicao_abandono_de_Ativa_para_Abandono()
    {
        var matricula = NovaMatricula();

        matricula.RegistrarAbandono();

        matricula.Situacao.Should().Be(SituacaoMatricula.Abandono);
    }

    [Fact] // Ativa --RegistrarSituacaoDoAluno--> Ativa (nao altera a situacao).
    public void Transicao_registrar_situacao_do_aluno_mantem_Ativa()
    {
        var matricula = NovaMatricula();

        matricula.RegistrarSituacaoDoAluno(Rendimento.Reprovado, Movimento.Abandono);

        matricula.Situacao.Should().Be(SituacaoMatricula.Ativa);
    }

    // ---------- Persistencia, auditoria e isolamento ----------

    [Fact] // Cenario 5: encerramento por conclusao persiste e registra trilha de auditoria.
    public async Task Cenario_5_encerramento_persiste_com_auditoria()
    {
        MatriculaId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var matricula = NovaMatricula();
            id = matricula.Id;
            contexto.Matriculas.Add(matricula);
            await contexto.SaveChangesAsync();

            matricula.Concluir();
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var matricula = await contexto.Matriculas.SingleAsync(m => m.Id == id);
            matricula.Situacao.Should().Be(SituacaoMatricula.Concluida);
            matricula.TenantId.Should().Be(TenantA);

            (await contexto.AuditTrail.ToListAsync())
                .Should().NotBeEmpty("o interceptor de auditoria deve registrar as mutacoes da matricula");
        }
    }

    [Fact] // Cenario 8: SituacaoDoAluno (owned) persiste e e recarregada.
    public async Task Cenario_8_situacao_do_aluno_persiste_como_owned()
    {
        MatriculaId id;
        await using (var contexto = CriarContexto(TenantA))
        {
            var matricula = NovaMatricula();
            id = matricula.Id;
            matricula.RegistrarSituacaoDoAluno(Rendimento.Aprovado, Movimento.SemMovimento);
            contexto.Matriculas.Add(matricula);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var matricula = await contexto.Matriculas.SingleAsync(m => m.Id == id);
            matricula.SituacaoDoAluno.Should().NotBeNull();
            matricula.SituacaoDoAluno!.Rendimento.Should().Be(Rendimento.Aprovado);
        }
    }

    [Fact] // I-7/B-10 + Cenario 10: consulta e tenant-scoped (Global Query Filter).
    public async Task Cenario_10_consulta_e_isolada_por_tenant()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Matriculas.Add(NovaMatricula());
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Matriculas.ToListAsync()).Should().BeEmpty();
        }
    }
}
