using FluentAssertions;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;
using Xunit;

namespace Tensorroot.Gov.Modules.Educacao.Tests;

/// <summary>
/// Testes-chave de dominio do agregado <see cref="Turma"/> (Onda 1): criacao valida (I-T1), a
/// invariante de vaga (I-T2: matriculados &lt;= vagas), o ajuste de vagas nunca abaixo do enturmado
/// (I-T3) e o encerramento que exige turma sem matricula (I-T5). E a regressao do stub
/// <c>PossuiVagaAsync</c> (agora reflete contagem real, nao <c>Guid.Empty</c>). Dominio puro.
/// </summary>
public sealed class TurmaDominioTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Turma TurmaAberta(int vagas = 2)
    {
        var turma = Turma.Criar(Tenant, EscolaId.New(), 2026, Etapa.Fundamental1, "1º ano", Turno.Matutino, vagas);
        turma.Abrir();
        return turma;
    }

    [Fact]
    public void Criar_turma_nasce_planejada_com_vagas_disponiveis_e_emite_evento()
    {
        var turma = Turma.Criar(Tenant, EscolaId.New(), 2026, Etapa.Fundamental1, "1º ano", Turno.Matutino, 30);

        turma.Situacao.Should().Be(SituacaoTurma.Planejada);
        turma.Matriculados.Should().Be(0);
        turma.VagasDisponiveis.Should().Be(30);
        turma.PossuiVaga.Should().BeFalse("turma planejada ainda nao enturma");
        turma.DomainEvents.Should().ContainSingle(e => e is TurmaCriada);
    }

    [Fact]
    public void Criar_com_vagas_nao_positivas_deve_rejeitar_IT1()
    {
        var acao = () => Turma.Criar(Tenant, EscolaId.New(), 2026, Etapa.Fundamental1, "1º ano", Turno.Matutino, 0);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Enturmar_ate_o_limite_e_alem_respeita_IT2()
    {
        var turma = TurmaAberta(vagas: 2);

        turma.PossuiVaga.Should().BeTrue();
        turma.IncrementarMatriculados();
        turma.IncrementarMatriculados();

        turma.Matriculados.Should().Be(2);
        turma.VagasDisponiveis.Should().Be(0);
        turma.PossuiVaga.Should().BeFalse();
        var acao = () => turma.IncrementarMatriculados();
        acao.Should().Throw<InvalidOperationException>("turma cheia nao admite enturmacao (I-T2)");
    }

    [Fact]
    public void Enturmar_decrementar_reflete_contagem_real_regressao_do_stub()
    {
        var turma = TurmaAberta(vagas: 1);
        turma.IncrementarMatriculados();
        turma.PossuiVaga.Should().BeFalse();

        turma.DecrementarMatriculados();

        turma.Matriculados.Should().Be(0);
        turma.PossuiVaga.Should().BeTrue("vaga liberada pela transferencia/encerramento de matricula");
    }

    [Fact]
    public void AjustarVagas_abaixo_de_matriculados_deve_rejeitar_IT3()
    {
        var turma = TurmaAberta(vagas: 3);
        turma.IncrementarMatriculados();
        turma.IncrementarMatriculados();

        var acao = () => turma.AjustarVagas(1);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Encerrar_turma_com_matricula_ativa_deve_rejeitar_IT5()
    {
        var turma = TurmaAberta(vagas: 2);
        turma.IncrementarMatriculados();

        var acao = () => turma.Encerrar();

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Encerrar_turma_vazia_deve_ser_aceito_e_emitir_evento()
    {
        var turma = TurmaAberta(vagas: 2);

        turma.Encerrar();

        turma.Situacao.Should().Be(SituacaoTurma.Encerrada);
        turma.DomainEvents.Should().Contain(e => e is TurmaEncerrada);
    }
}
