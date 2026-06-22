using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Tribuna;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// Cobertura do agregado <see cref="TribunaSessao"/>: inscricao idempotente, exclusao mutua,
/// cronometro server-side (pausas, tempo e excedente) e isolamento por tenant (T-1..T-9).
/// </summary>
public sealed class TribunaTests : LegislativoTestBase
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 22, 14, 0, 0, TimeSpan.Zero);

    private static Sessao SessaoAberta()
    {
        var sessao = Sessao.Agendar(TenantA, TipoSessao.Ordinaria, DataHora.De(T0), totalMembros: 9);
        for (var i = 0; i < 5; i++)
        {
            sessao.RegistrarPresenca(VereadorId.New(), T0);
        }

        sessao.Abrir();
        return sessao;
    }

    private static TribunaSessao NovaTribuna()
        => TribunaSessao.Abrir(TenantA, SessaoAberta(), TimeSpan.FromMinutes(5));

    [Fact] // T-1: tribuna so para sessao nao terminal.
    public void Tribuna_so_para_sessao_nao_terminal()
    {
        var sessao = SessaoAberta();
        sessao.Encerrar();

        ((Action)(() => TribunaSessao.Abrir(TenantA, sessao)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // T-2: inscricao idempotente por vereador+fase.
    public void Inscrever_idempotente_por_vereador_e_fase()
    {
        var tribuna = NovaTribuna();
        var vereador = VereadorId.New();

        var primeira = tribuna.Inscrever(vereador, FaseUsoPalavra.GrandeExpediente);
        var segunda = tribuna.Inscrever(vereador, FaseUsoPalavra.GrandeExpediente);

        segunda.Id.Should().Be(primeira.Id);
        tribuna.Inscricoes.Should().ContainSingle();
    }

    [Fact] // T-3/T-4: iniciar fala com outro orador em uso lanca (exclusao mutua).
    public void IniciarFala_com_outro_orador_em_uso_lanca()
    {
        var tribuna = NovaTribuna();
        var a = tribuna.Inscrever(VereadorId.New(), FaseUsoPalavra.GrandeExpediente);
        var b = tribuna.Inscrever(VereadorId.New(), FaseUsoPalavra.GrandeExpediente);

        tribuna.IniciarFala(a.Id, T0);

        ((Action)(() => tribuna.IniciarFala(b.Id, T0.AddSeconds(1))))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // T-5: pausar e retomar descontam o intervalo do tempo utilizado.
    public void Pausar_e_retomar_descontam_intervalo_do_tempo_utilizado()
    {
        var tribuna = NovaTribuna();
        var inscricao = tribuna.Inscrever(VereadorId.New(), FaseUsoPalavra.GrandeExpediente, TimeSpan.FromMinutes(5));

        tribuna.IniciarFala(inscricao.Id, T0);
        tribuna.PausarFala(inscricao.Id, T0.AddMinutes(1));     // falou 1 min
        tribuna.RetomarFala(inscricao.Id, T0.AddMinutes(3));    // 2 min de pausa
        tribuna.EncerrarFala(inscricao.Id, T0.AddMinutes(4));   // mais 1 min falando

        // Total decorrido 4 min - 2 min de pausa = 2 min utilizados.
        inscricao.TempoUtilizado.Should().Be(TimeSpan.FromMinutes(2));
        inscricao.Excedente.Should().Be(TimeSpan.Zero);
    }

    [Fact] // T-6: encerrar calcula tempo e excedente.
    public void EncerrarFala_calcula_tempo_e_excedente()
    {
        var tribuna = NovaTribuna();
        var inscricao = tribuna.Inscrever(VereadorId.New(), FaseUsoPalavra.GrandeExpediente, TimeSpan.FromMinutes(5));

        tribuna.IniciarFala(inscricao.Id, T0);
        tribuna.EncerrarFala(inscricao.Id, T0.AddMinutes(7));

        inscricao.TempoUtilizado.Should().Be(TimeSpan.FromMinutes(7));
        inscricao.Excedente.Should().Be(TimeSpan.FromMinutes(2)); // 7 - 5 concedidos.
        tribuna.OradorEmUso.Should().BeNull(); // exclusao mutua liberada.
    }

    [Fact] // T-7: cancelar inscricao de orador em uso lanca.
    public void CancelarInscricao_de_orador_em_uso_lanca()
    {
        var tribuna = NovaTribuna();
        var inscricao = tribuna.Inscrever(VereadorId.New(), FaseUsoPalavra.GrandeExpediente);
        tribuna.IniciarFala(inscricao.Id, T0);

        ((Action)(() => tribuna.CancelarInscricao(inscricao.Id)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // Persistencia do cronometro (timestamps + pausas) + isolamento por tenant.
    public async Task Persiste_cronometro_e_pausas_com_isolamento()
    {
        TribunaSessaoId tribunaId;
        SessaoId sessaoId;
        await using (var contexto = CriarContexto(TenantA))
        {
            var sessao = SessaoAberta();
            sessaoId = sessao.Id;
            contexto.Sessoes.Add(sessao);

            var tribuna = TribunaSessao.Abrir(TenantA, sessao, TimeSpan.FromMinutes(5));
            tribunaId = tribuna.Id;
            var inscricao = tribuna.Inscrever(VereadorId.New(), FaseUsoPalavra.GrandeExpediente);
            tribuna.IniciarFala(inscricao.Id, T0);
            tribuna.PausarFala(inscricao.Id, T0.AddMinutes(1));
            tribuna.RetomarFala(inscricao.Id, T0.AddMinutes(2));
            contexto.Tribunas.Add(tribuna);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var tribuna = await contexto.Tribunas.SingleAsync(t => t.SessaoId == sessaoId);
            var inscricao = tribuna.Inscricoes.Single();
            inscricao.IniciadoEm.Should().Be(T0);
            inscricao.Pausas.Should().ContainSingle()
                .Which.Duracao.Should().Be(TimeSpan.FromMinutes(1));
        }

        await using (var contexto = CriarContexto(TenantB))
        {
            (await contexto.Tribunas.ToListAsync()).Should().BeEmpty();
        }
    }
}
