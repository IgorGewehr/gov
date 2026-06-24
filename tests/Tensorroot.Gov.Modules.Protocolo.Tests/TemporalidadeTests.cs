using FluentAssertions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Protocolo.Infrastructure.Protocolo.Temporalidade;
using Xunit;

namespace Tensorroot.Gov.Modules.Protocolo.Tests;

/// <summary>
/// Cobertura do motor de temporalidade/destinacao (Peca 2 / W9.4): calculo deterministico das 3 fases
/// e invariantes I-T1..I-T7 da ficha de destinacao. Espelha Temporalidade.rules.md.
/// </summary>
public sealed class TemporalidadeTests
{
    private const string HashTermo = "a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e";
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static RegraTemporalidade RegraEliminacao()
        => RegraTemporalidade.Criar("040", 2, 5, Destinacao.Eliminacao, EventoContagem.DataArquivamento, "TTD teste");

    private static RegraTemporalidade RegraPermanente()
        => RegraTemporalidade.Criar("020", 5, 10, Destinacao.GuardaPermanente, EventoContagem.DataArquivamento, "TTD teste");

    [Fact] // C-1: calculo das 3 fases (corrente=2, intermediaria=5, eliminacao).
    public void Motor_calcula_as_tres_fases()
    {
        var motor = new MotorTemporalidade();
        var plano = motor.Calcular(RegraEliminacao(), new DateOnly(2026, 1, 10));

        plano.FimGuardaCorrente.Should().Be(new DateOnly(2028, 1, 10));
        plano.FimGuardaIntermediaria.Should().Be(new DateOnly(2033, 1, 10));
        plano.Destinacao.Should().Be(Destinacao.Eliminacao);
        plano.DataAptidaoEliminacao.Should().Be(new DateOnly(2033, 1, 10));
    }

    [Fact] // C-2: guarda permanente nao tem data de aptidao a eliminacao.
    public void Motor_guarda_permanente_sem_aptidao()
    {
        var motor = new MotorTemporalidade();
        var plano = motor.Calcular(RegraPermanente(), new DateOnly(2026, 1, 10));

        plano.Destinacao.Should().Be(Destinacao.GuardaPermanente);
        plano.DataAptidaoEliminacao.Should().BeNull();
    }

    [Fact] // I-T1: nao eliminar antes do prazo.
    public void IT1_nao_elimina_antes_do_prazo()
    {
        var ficha = DestinacaoProcesso.Criar(
            Tenant, Guid.NewGuid(), "040",
            new DateOnly(2028, 1, 10), new DateOnly(2033, 1, 10),
            Destinacao.Eliminacao, new DateOnly(2033, 1, 10));

        var acao = () => ficha.AutorizarEliminacao(Guid.NewGuid(), new DateOnly(2030, 1, 1));

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-T2: guarda permanente nunca elimina.
    public void IT2_guarda_permanente_nunca_elimina()
    {
        var ficha = DestinacaoProcesso.Criar(
            Tenant, Guid.NewGuid(), "020",
            new DateOnly(2031, 1, 10), new DateOnly(2041, 1, 10),
            Destinacao.GuardaPermanente, dataAptidaoEliminacao: null);

        ficha.Estado.Should().Be(EstadoDestinacao.Permanente);
        ((Action)(() => ficha.AutorizarEliminacao(Guid.NewGuid(), new DateOnly(2099, 1, 1))))
            .Should().Throw<InvalidOperationException>();
        ((Action)(() => ficha.MarcarEliminado(Hash.De(HashTermo), "edital/2099", new DateOnly(2099, 1, 1))))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // I-T4 + fluxo feliz: apto -> autorizado -> eliminado com termo assinado/carimbado.
    public void IT4_eliminacao_exige_termo_e_completa_fluxo()
    {
        var ficha = DestinacaoProcesso.Criar(
            Tenant, Guid.NewGuid(), "040",
            new DateOnly(2028, 1, 10), new DateOnly(2033, 1, 10),
            Destinacao.Eliminacao, new DateOnly(2033, 1, 10));

        var hoje = new DateOnly(2033, 6, 1);
        ficha.AvaliarAptidao(hoje).Should().BeTrue();
        ficha.Estado.Should().Be(EstadoDestinacao.AptoEliminar);

        ficha.AutorizarEliminacao(Guid.NewGuid(), hoje);
        ficha.Estado.Should().Be(EstadoDestinacao.EliminacaoAutorizada);

        ficha.MarcarEliminado(Hash.De(HashTermo), "edital/2033", hoje);
        ficha.Estado.Should().Be(EstadoDestinacao.Eliminado);
        ficha.TermoEliminacaoHash.Should().Be(HashTermo);
    }

    [Fact] // I-T5: classe deve existir no plano do tenant (Contem).
    public void IT5_classe_inexistente_no_plano_e_detectada()
    {
        var plano = PlanoDeClassificacao.Criar(Tenant, "Plano 2026");
        plano.AdicionarClasse("040", "Patrimonio", AtividadeMeioOuFim.Meio);

        plano.Contem("040").Should().BeTrue();
        plano.Contem("999").Should().BeFalse();
    }
}
