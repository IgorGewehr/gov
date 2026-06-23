using FluentAssertions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// Bordas de apuracao e integridade do conjunto de votos (B-1..B-8): unicidade do voto em toda
/// modalidade (BUG-2), teto de votos e quorum de deliberacao (BUG-7), presentes &lt;= membros (BUG-8)
/// e as fronteiras de maioria simples/absoluta/qualificada que protegem a apuracao na demo.
/// </summary>
public sealed class BordasVotacaoTests
{
    private static readonly DateTimeOffset Quando = new(2026, 6, 22, 15, 0, 0, TimeSpan.Zero);

    private static Votacao NovaVotacao(
        TipoVotacao tipo = TipoVotacao.Simbolica,
        MaioriaExigida maioria = MaioriaExigida.Simples,
        int totalMembros = 11,
        int presentes = 9,
        int turno = 1)
        => Votacao.Iniciar(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
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

    // ---------- BUG-2: unicidade do voto em toda modalidade ----------

    [Fact] // B-1: secreta — o mesmo vereador nao vota duas vezes (esperar excecao; VotosSim == 1).
    public void Secreta_mesmo_vereador_nao_vota_duas_vezes()
    {
        var votacao = NovaVotacao(TipoVotacao.Secreta);
        var vereador = VereadorId.New();
        votacao.RegistrarVoto(VotoId.New(), vereador, SentidoVoto.Sim, Quando);

        ((Action)(() => votacao.RegistrarVoto(VotoId.New(), vereador, SentidoVoto.Sim, Quando)))
            .Should().Throw<InvalidOperationException>();
        votacao.VotosSim.Should().Be(1);
    }

    [Fact] // BUG-2: simbolica — o mesmo vereador nao vota duas vezes.
    public void Simbolica_mesmo_vereador_nao_vota_duas_vezes()
    {
        var votacao = NovaVotacao(TipoVotacao.Simbolica);
        var vereador = VereadorId.New();
        votacao.RegistrarVoto(VotoId.New(), vereador, SentidoVoto.Sim, Quando);

        ((Action)(() => votacao.RegistrarVoto(VotoId.New(), vereador, SentidoVoto.Nao, Quando)))
            .Should().Throw<InvalidOperationException>();
        votacao.Votos.Should().ContainSingle();
    }

    [Fact] // I-12 (sigilo): secreta mantem placar agregado mas a identidade so existe para unicidade.
    public void Secreta_mantem_placar_mas_unicidade_por_vereador()
    {
        var votacao = NovaVotacao(TipoVotacao.Secreta, presentes: 5);
        RegistrarVotos(votacao, SentidoVoto.Sim, 3);

        votacao.VotosSim.Should().Be(3);
        votacao.Votos.Should().HaveCount(3);
    }

    // ---------- BUG-7: teto de votos e quorum de deliberacao ----------

    [Fact] // B-2: nao aceita mais votos que presentes (teto) — "13 votos numa Camara de 11" recusado.
    public void Nao_aceita_mais_votos_que_presentes()
    {
        var votacao = NovaVotacao(totalMembros: 11, presentes: 3);
        RegistrarVotos(votacao, SentidoVoto.Sim, 3); // preenche os 3 presentes

        ((Action)(() => votacao.RegistrarVoto(VotoId.New(), VereadorId.New(), SentidoVoto.Sim, Quando)))
            .Should().Throw<InvalidOperationException>();
        votacao.Votos.Should().HaveCount(3);
    }

    [Fact] // B-2: o total de votos nunca excede o total de membros (decorre do teto por presentes).
    public void Total_de_votos_nao_excede_total_membros()
    {
        var votacao = NovaVotacao(totalMembros: 11, presentes: 11);
        RegistrarVotos(votacao, SentidoVoto.Sim, 11);

        votacao.Votos.Count.Should().Be(11);
        ((Action)(() => votacao.RegistrarVoto(VotoId.New(), VereadorId.New(), SentidoVoto.Sim, Quando)))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact] // B-3: encerrar sem quorum minimo marca Prejudicado (totalMembros 11 -> quorum 6, presentes 3).
    public void Encerrar_sem_quorum_minimo_marca_prejudicada()
    {
        var votacao = NovaVotacao(totalMembros: 11, presentes: 3);
        RegistrarVotos(votacao, SentidoVoto.Sim, 3);

        var resultado = votacao.Encerrar();

        resultado.Should().Be(ResultadoVotacao.Prejudicado);
        votacao.Resultado.Should().Be(ResultadoVotacao.Prejudicado);
    }

    [Fact] // BUG-8(a): presentes maior que total de membros e estado impossivel — rejeitado.
    public void Iniciar_rejeita_presentes_maior_que_total_membros()
    {
        ((Action)(() => NovaVotacao(totalMembros: 11, presentes: 20)))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] // BUG-8(a): presentes == total de membros e valido (todos presentes).
    public void Iniciar_aceita_presentes_igual_ao_total_de_membros()
    {
        ((Action)(() => NovaVotacao(totalMembros: 11, presentes: 11))).Should().NotThrow();
    }

    // ---------- Fronteiras de maioria ----------

    [Fact] // B-5: maioria simples no limite com presentes impar (7 -> 4 aprova; 3 rejeita).
    public void Maioria_simples_presentes_impar_no_limite()
    {
        var aprovado = NovaVotacao(maioria: MaioriaExigida.Simples, presentes: 7);
        RegistrarVotos(aprovado, SentidoVoto.Sim, 4);
        aprovado.Encerrar().Should().Be(ResultadoVotacao.Aprovado);

        var rejeitado = NovaVotacao(maioria: MaioriaExigida.Simples, presentes: 7);
        RegistrarVotos(rejeitado, SentidoVoto.Sim, 3);
        rejeitado.Encerrar().Should().Be(ResultadoVotacao.Rejeitado);
    }

    [Fact] // B-6a: maioria absoluta com membros par no limite (10 -> 6 aprova; 5 rejeita).
    public void Absoluta_membros_par_limite()
    {
        var aprovado = NovaVotacao(maioria: MaioriaExigida.Absoluta, totalMembros: 10, presentes: 10);
        RegistrarVotos(aprovado, SentidoVoto.Sim, 6);
        aprovado.Encerrar().Should().Be(ResultadoVotacao.Aprovado);

        var rejeitado = NovaVotacao(maioria: MaioriaExigida.Absoluta, totalMembros: 10, presentes: 10);
        RegistrarVotos(rejeitado, SentidoVoto.Sim, 5);
        rejeitado.Encerrar().Should().Be(ResultadoVotacao.Rejeitado);
    }

    [Fact] // B-6b: qualificada quando 2/3 nao e inteiro (membros 9 -> ceil(6) -> 6 aprova; 5 rejeita).
    public void Qualificada_quando_dois_tercos_e_inteiro()
    {
        var aprovado = NovaVotacao(maioria: MaioriaExigida.Qualificada, totalMembros: 9, presentes: 9);
        RegistrarVotos(aprovado, SentidoVoto.Sim, 6);
        aprovado.Encerrar().Should().Be(ResultadoVotacao.Aprovado);

        var rejeitado = NovaVotacao(maioria: MaioriaExigida.Qualificada, totalMembros: 9, presentes: 9);
        RegistrarVotos(rejeitado, SentidoVoto.Sim, 5);
        rejeitado.Encerrar().Should().Be(ResultadoVotacao.Rejeitado);
    }

    [Fact] // B-7: abstencao nao reduz a base da maioria simples (presentes 9, sim 4, abstencao 5 -> rejeita).
    public void Abstencao_nao_reduz_base_da_maioria_simples()
    {
        var votacao = NovaVotacao(maioria: MaioriaExigida.Simples, presentes: 9);
        RegistrarVotos(votacao, SentidoVoto.Sim, 4);
        RegistrarVotos(votacao, SentidoVoto.Abstencao, 5);

        // base continua sendo presentes (9): 4 nao supera 9/2.
        votacao.Encerrar().Should().Be(ResultadoVotacao.Rejeitado);
    }

    [Fact] // MaioriaAtingida: o placar projeta a MAIOR maioria efetivamente alcancada (vinculo BUG-1).
    public void MaioriaAtingida_reflete_o_placar_real_nao_o_exigido()
    {
        // Exigida Simples, mas placar alcanca a Absoluta (>= membros/2 + 1).
        var votacao = NovaVotacao(maioria: MaioriaExigida.Simples, totalMembros: 11, presentes: 11);
        RegistrarVotos(votacao, SentidoVoto.Sim, 6);

        votacao.MaioriaAtingida().Should().Be(MaioriaExigida.Absoluta);

        // Sem nenhuma maioria atingida -> null.
        var fraca = NovaVotacao(maioria: MaioriaExigida.Simples, totalMembros: 11, presentes: 9);
        RegistrarVotos(fraca, SentidoVoto.Sim, 2);
        fraca.MaioriaAtingida().Should().BeNull();
    }
}
