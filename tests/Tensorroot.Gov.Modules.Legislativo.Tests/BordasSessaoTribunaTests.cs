using FluentAssertions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Tribuna;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// Bordas de sessao/tribuna: cronometro nunca negativo (BUG-4), re-inscricao apos concluir (BUG-6),
/// fronteiras de quorum, excedente no limite, transicoes de pausa indevidas e verificacao de quorum
/// em sessao terminal (evento espurio).
/// </summary>
public sealed class BordasSessaoTribunaTests
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 22, 14, 0, 0, TimeSpan.Zero);

    private static Sessao SessaoComMembros(int totalMembros, int presentes)
    {
        var sessao = Sessao.Agendar(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            TipoSessao.Ordinaria,
            DataHora.De(T0),
            totalMembros);
        for (var i = 0; i < presentes; i++)
        {
            sessao.RegistrarPresenca(VereadorId.New(), T0);
        }

        return sessao;
    }

    private static TribunaSessao NovaTribuna()
    {
        var sessao = SessaoComMembros(9, 5);
        sessao.Abrir();
        return TribunaSessao.Abrir(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            sessao,
            TimeSpan.FromMinutes(5));
    }

    // ---------- BUG-4: cronometro nunca negativo ----------

    [Fact] // Item 16: encerrar com momento anterior ao inicio e rejeitado.
    public void EncerrarFala_com_momento_anterior_ao_inicio_rejeita()
    {
        var tribuna = NovaTribuna();
        var inscricao = tribuna.Inscrever(VereadorId.New(), FaseUsoPalavra.GrandeExpediente);
        tribuna.IniciarFala(inscricao.Id, T0);

        ((Action)(() => tribuna.EncerrarFala(inscricao.Id, T0.AddSeconds(-42))))
            .Should().Throw<ArgumentException>();
    }

    [Fact] // Item 16: TempoUtilizado nunca e negativo (clamp em zero) mesmo com pausa no limite.
    public void TempoUtilizado_nunca_negativo()
    {
        var tribuna = NovaTribuna();
        var inscricao = tribuna.Inscrever(VereadorId.New(), FaseUsoPalavra.GrandeExpediente);
        tribuna.IniciarFala(inscricao.Id, T0);
        // Pausa cobre toda a fala: (fim - inicio) - pausa = 0, nunca negativo.
        tribuna.PausarFala(inscricao.Id, T0);
        tribuna.RetomarFala(inscricao.Id, T0.AddMinutes(2));
        tribuna.EncerrarFala(inscricao.Id, T0.AddMinutes(2));

        inscricao.TempoUtilizado.Should().Be(TimeSpan.Zero);
        inscricao.TempoUtilizado!.Value.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact] // BUG-4: pausar antes do inicio da fala e rejeitado (monotonicidade).
    public void PausarFala_com_momento_anterior_ao_inicio_rejeita()
    {
        var tribuna = NovaTribuna();
        var inscricao = tribuna.Inscrever(VereadorId.New(), FaseUsoPalavra.GrandeExpediente);
        tribuna.IniciarFala(inscricao.Id, T0);

        ((Action)(() => tribuna.PausarFala(inscricao.Id, T0.AddSeconds(-1))))
            .Should().Throw<ArgumentException>();
    }

    // ---------- BUG-6: re-inscricao apos concluir ----------

    [Fact] // Item 17: re-inscrever apos concluir cria NOVA inscricao na mesma fase (nao devolve a morta).
    public void Reinscrever_apos_concluir_cria_nova_inscricao_na_mesma_fase()
    {
        var tribuna = NovaTribuna();
        var vereador = VereadorId.New();
        var primeira = tribuna.Inscrever(vereador, FaseUsoPalavra.GrandeExpediente);
        tribuna.IniciarFala(primeira.Id, T0);
        tribuna.EncerrarFala(primeira.Id, T0.AddMinutes(3));

        var segunda = tribuna.Inscrever(vereador, FaseUsoPalavra.GrandeExpediente);

        segunda.Id.Should().NotBe(primeira.Id);
        segunda.Situacao.Should().Be(SituacaoInscricao.Inscrito);
        tribuna.Inscricoes.Should().HaveCount(2);

        // E a nova inscricao consegue iniciar fala (a regressao do BUG-6 travava aqui).
        ((Action)(() => tribuna.IniciarFala(segunda.Id, T0.AddMinutes(4)))).Should().NotThrow();
    }

    [Fact] // BUG-6: enquanto pendente (Inscrito), a re-inscricao continua idempotente (nao duplica).
    public void Reinscrever_enquanto_inscrito_continua_idempotente()
    {
        var tribuna = NovaTribuna();
        var vereador = VereadorId.New();

        var a = tribuna.Inscrever(vereador, FaseUsoPalavra.GrandeExpediente);
        var b = tribuna.Inscrever(vereador, FaseUsoPalavra.GrandeExpediente);

        b.Id.Should().Be(a.Id);
        tribuna.Inscricoes.Should().ContainSingle();
    }

    // ---------- Item 20: excedente no limite ----------

    [Fact] // Excedente exatamente no limite e zero; um tick acima e positivo.
    public void Excedente_no_limite_e_zero_e_um_tick_acima_e_positivo()
    {
        var tribuna = NovaTribuna();
        var noLimite = tribuna.Inscrever(VereadorId.New(), FaseUsoPalavra.GrandeExpediente, TimeSpan.FromMinutes(5));
        tribuna.IniciarFala(noLimite.Id, T0);
        tribuna.EncerrarFala(noLimite.Id, T0.AddMinutes(5)); // exatamente o concedido
        noLimite.Excedente.Should().Be(TimeSpan.Zero);

        var acima = tribuna.Inscrever(VereadorId.New(), FaseUsoPalavra.GrandeExpediente, TimeSpan.FromMinutes(5));
        tribuna.IniciarFala(acima.Id, T0.AddMinutes(10));
        tribuna.EncerrarFala(acima.Id, T0.AddMinutes(15).AddTicks(1)); // um tick acima
        acima.Excedente!.Value.Should().BeGreaterThan(TimeSpan.Zero);
    }

    // ---------- Item 21: transicoes de pausa indevidas ----------

    [Fact] // Pausar duas vezes lanca.
    public void Pausar_duas_vezes_lanca()
    {
        var tribuna = NovaTribuna();
        var inscricao = tribuna.Inscrever(VereadorId.New(), FaseUsoPalavra.GrandeExpediente);
        tribuna.IniciarFala(inscricao.Id, T0);
        tribuna.PausarFala(inscricao.Id, T0.AddMinutes(1));

        ((Action)(() => tribuna.PausarFala(inscricao.Id, T0.AddMinutes(2))))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // Retomar sem pausa lanca.
    public void Retomar_sem_pausa_lanca()
    {
        var tribuna = NovaTribuna();
        var inscricao = tribuna.Inscrever(VereadorId.New(), FaseUsoPalavra.GrandeExpediente);
        tribuna.IniciarFala(inscricao.Id, T0);

        ((Action)(() => tribuna.RetomarFala(inscricao.Id, T0.AddMinutes(1))))
            .Should().Throw<InvalidOperationException>();
    }

    // ---------- Item 19: fronteiras de quorum ----------

    [Fact] // Quorum minimo exato com membros par (10 -> quorum 6): abrir com 6 funciona.
    public void Quorum_minimo_exato_membros_par()
    {
        var sessao = SessaoComMembros(totalMembros: 10, presentes: 6);
        sessao.QuorumInstalacao.Should().Be(6);

        ((Action)sessao.Abrir).Should().NotThrow();
        sessao.Situacao.Should().Be(SituacaoSessao.Aberta);
    }

    [Fact] // Abrir com quorum menos um falha (10 -> 5 presentes).
    public void Abrir_com_quorum_menos_um_falha()
    {
        var sessao = SessaoComMembros(totalMembros: 10, presentes: 5);

        ((Action)sessao.Abrir).Should().Throw<InvalidOperationException>();
    }

    // ---------- Item 22: verificacao de quorum em sessao terminal ----------

    [Fact] // VerificarQuorum em sessao terminal nao emite QuorumVerificado (evento espurio).
    public void VerificarQuorum_em_sessao_terminal_nao_emite_evento()
    {
        var sessao = SessaoComMembros(totalMembros: 9, presentes: 5);
        sessao.Abrir();
        sessao.Encerrar(); // terminal

        ((Action)(() => sessao.VerificarQuorum())).Should().Throw<InvalidOperationException>();
        sessao.DomainEvents.OfType<QuorumVerificado>().Should().BeEmpty();
    }
}
