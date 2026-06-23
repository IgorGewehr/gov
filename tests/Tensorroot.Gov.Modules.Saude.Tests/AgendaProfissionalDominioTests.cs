using FluentAssertions;
using Tensorroot.Gov.Modules.Saude.Domain.Agendamento;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;
using Xunit;

namespace Tensorroot.Gov.Modules.Saude.Tests;

/// <summary>
/// Testes-chave do dominio de AGENDAMENTO (Onda 2 Saude, design §3.7): expansao da grade em vagas,
/// invariante ANTI-OVERBOOKING (uma vaga Livre admite uma unica ocupacao), e liberacao de vaga ao
/// cancelar/faltar (reentra no pool). Dominio puro (sem I/O).
/// </summary>
public sealed class AgendaProfissionalDominioTests
{
    private static AgendaProfissional AgendaPublicada(int duracaoSlotMin = 30, int capacidade = 1)
    {
        var agenda = AgendaProfissional.Abrir(
            Guid.NewGuid(),
            new ProfissionalId(Guid.NewGuid()),
            new EstabelecimentoId(Guid.NewGuid()),
            TipoAtendimentoAgenda.Consulta,
            new DateOnly(2026, 7, 1),
            new TimeOnly(8, 0),
            new TimeOnly(9, 0),
            duracaoSlotMin,
            capacidade);
        agenda.Publicar(TimeSpan.FromHours(-3));
        return agenda;
    }

    [Fact]
    public void Publicar_expande_a_grade_em_vagas_livres()
    {
        // 08:00-09:00, slot 30min, capacidade 1 → 2 vagas.
        var agenda = AgendaPublicada();

        agenda.Situacao.Should().Be(SituacaoAgenda.Aberta);
        agenda.Vagas.Should().HaveCount(2);
        agenda.Vagas.Should().OnlyContain(v => v.Situacao == SituacaoVaga.Livre);
    }

    [Fact]
    public void Sem_overbooking_segunda_ocupacao_da_mesma_vaga_falha()
    {
        var agenda = AgendaPublicada();
        var vagaId = agenda.Vagas.First().Id;

        var primeira = agenda.OcuparVaga(vagaId);
        primeira.Should().Be(new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.FromHours(-3)));
        agenda.Vagas.First().Situacao.Should().Be(SituacaoVaga.Ocupada);

        // 2a marcacao na MESMA vaga: rejeitada (anti-overbooking).
        var segunda = () => agenda.OcuparVaga(vagaId);
        segunda.Should().Throw<InvalidOperationException>().WithMessage("*indisponivel*");
    }

    [Fact]
    public void Liberar_vaga_devolve_ao_pool_apos_cancelamento()
    {
        var agenda = AgendaPublicada();
        var vagaId = agenda.Vagas.First().Id;
        agenda.OcuparVaga(vagaId);

        agenda.LiberarVaga(vagaId);

        agenda.Vagas.First().Situacao.Should().Be(SituacaoVaga.Livre);
        // Apos liberar, a vaga aceita nova marcacao.
        var acao = () => agenda.OcuparVaga(vagaId);
        acao.Should().NotThrow();
    }

    [Fact]
    public void Bloquear_dia_impede_marcacao_e_reabrir_restaura()
    {
        var agenda = AgendaPublicada();

        agenda.BloquearDia("Profissional em ferias.");
        agenda.Situacao.Should().Be(SituacaoAgenda.Bloqueada);
        var marcarBloqueada = () => agenda.OcuparVaga(agenda.Vagas.First().Id);
        marcarBloqueada.Should().Throw<InvalidOperationException>().WithMessage("*nao esta aberta*");

        agenda.ReabrirDia();
        agenda.Situacao.Should().Be(SituacaoAgenda.Aberta);
        agenda.Vagas.Should().OnlyContain(v => v.Situacao == SituacaoVaga.Livre);
    }
}
