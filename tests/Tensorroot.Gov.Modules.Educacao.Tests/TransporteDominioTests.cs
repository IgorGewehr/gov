using FluentAssertions;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.Modules.Educacao.Domain.Transporte;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;
using Xunit;

namespace Tensorroot.Gov.Modules.Educacao.Tests;

/// <summary>
/// Testes-chave de dominio do agregado <see cref="RotaTransporte"/> (Onda 3b — Transporte/PNATE):
/// criacao com coerencia veiculo x modalidade (I-R2), vinculo de alunos sem duplicidade (I-R4), guarda de
/// rota encerrada (I-R1) e ativacao exigindo alunos (I-R5). Dominio puro.
/// </summary>
public sealed class TransporteDominioTests
{
    private static readonly Guid Tenant = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private static readonly EscolaId Escola = new(Guid.Parse("99999999-9999-9999-9999-999999999999"));

    private static RotaTransporte RotaPropria()
        => RotaTransporte.Criar(Tenant, Escola, "Linha Rural Norte", Turno.Matutino, ModalidadeTransporte.Proprio, Guid.NewGuid(), 32.5m);

    [Fact]
    public void Criar_rota_propria_com_veiculo_deve_nascer_planejada_e_emitir_evento()
    {
        var rota = RotaPropria();

        rota.Situacao.Should().Be(SituacaoRotaTransporte.Planejada);
        rota.TenantId.Should().Be(Tenant);
        rota.VeiculoId.Should().NotBeNull();
        rota.DomainEvents.Should().ContainSingle(e => e is RotaTransporteCriada);
    }

    [Fact]
    public void Criar_rota_propria_sem_veiculo_deve_falhar()
    {
        var act = () => RotaTransporte.Criar(
            Tenant, Escola, "Linha Sem Veiculo", Turno.Matutino, ModalidadeTransporte.Proprio, null, 10m);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Criar_rota_terceirizada_com_veiculo_deve_falhar()
    {
        var act = () => RotaTransporte.Criar(
            Tenant, Escola, "Linha Terceirizada", Turno.Vespertino, ModalidadeTransporte.Terceirizado, Guid.NewGuid(), 10m);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Vincular_aluno_deve_adicionar_e_emitir_evento()
    {
        var rota = RotaPropria();
        var aluno = new AlunoId(Guid.NewGuid());

        rota.VincularAluno(aluno, null, "Parada KM 5");

        rota.TotalAtivos.Should().Be(1);
        rota.DomainEvents.Should().Contain(e => e is AlunoVinculadoRota);
    }

    [Fact]
    public void Vincular_mesmo_aluno_ativo_duas_vezes_deve_falhar()
    {
        var rota = RotaPropria();
        var aluno = new AlunoId(Guid.NewGuid());
        rota.VincularAluno(aluno, null, "Parada KM 5");

        var act = () => rota.VincularAluno(aluno, null, "Parada KM 5");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Ativar_rota_sem_alunos_deve_falhar()
    {
        var rota = RotaPropria();

        var act = () => rota.Ativar();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Ativar_rota_com_aluno_deve_transitar_para_ativa()
    {
        var rota = RotaPropria();
        rota.VincularAluno(new AlunoId(Guid.NewGuid()), null, "Parada KM 5");

        rota.Ativar();

        rota.Situacao.Should().Be(SituacaoRotaTransporte.Ativa);
    }

    [Fact]
    public void Vincular_aluno_em_rota_encerrada_deve_falhar()
    {
        var rota = RotaPropria();
        rota.Encerrar();

        var act = () => rota.VincularAluno(new AlunoId(Guid.NewGuid()), null, "Parada KM 5");

        act.Should().Throw<InvalidOperationException>();
    }
}
