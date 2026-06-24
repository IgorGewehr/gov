using FluentAssertions;
using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;
using Tensorroot.Gov.Modules.Convenios.Domain.Parametros;
using Tensorroot.Gov.SharedKernel.Tempo;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Convenios.Tests;

/// <summary>
/// Testes do agregado <see cref="ParceriaOsc"/> (fluxo B — MROSC, Lei 13.019/2014): fluxo
/// chamamento publico -&gt; termo (celebracao) -&gt; repasse com vinculo orcamentario -&gt; PC da OSC, e o
/// GATILHO de inadimplencia (LRF art. 48 / Lei 13.019 art. 48) que BLOQUEIA novos repasses (B-INV-9).
/// </summary>
public sealed class ParceriaOscTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Gestor = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Comissao = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly ICalendarioDiasUteis Calendario = new CalendarioCorridoFake();

    private static readonly ParametroPrazo PrazoEntregaPc = new(90, UnidadePrazo.DiasCorridos, "Lei 13.019/2014 art. 69");
    private static readonly ParametroPrazo PrazoAnalisePc = new(150, UnidadePrazo.DiasCorridos, "Lei 13.019/2014 art. 71");

    private static Osc OscHabilitada(DateOnly data) => Osc.Criar(
        Cnpj.Create("11222333000181"),
        "Instituto Parceiro",
        "Associacao civil sem fins lucrativos",
        experienciaPrevia: true,
        capacidadeTecnica: true,
        certidoes: new[] { new CertidaoRegularidade("FGTS", data.AddYears(1)) });

    /// <summary>Fluxo CHAMAMENTO publico homologado -&gt; termo de colaboracao celebrado, com 2 parcelas.</summary>
    private static ParceriaOsc CelebradaPorChamamento(out DateOnly inicioVigencia)
    {
        inicioVigencia = new DateOnly(2026, 1, 1);
        var selecao = FormaSelecao.PorChamamento(Guid.NewGuid(), "Edital 01/2026", homologado: false);
        var parceria = ParceriaOsc.IniciarSelecao(
            Tenant, OscHabilitada(inicioVigencia), TipoInstrumentoMrosc.TermoColaboracao, selecao);

        // Chamamento publico: homologa o edital (B-INV-1).
        parceria.HomologarChamamento();

        var parcelas = new[]
        {
            new ParcelaRepasse(1, Dinheiro.De(30_000m), inicioVigencia.AddMonths(1), "Inicio"),
            new ParcelaRepasse(2, Dinheiro.De(20_000m), inicioVigencia.AddMonths(4), "PC parcial 1 aprovada"),
        };
        var metas = new[] { new MetaOsc("Atender 100 familias", "Familias atendidas", "100") };

        parceria.RegistrarPlanoTrabalho("Servico socioassistencial", Dinheiro.De(50_000m), metas, parcelas);
        parceria.AprovarPlanoTrabalho();
        parceria.Celebrar(Vigencia.Criar(inicioVigencia, inicioVigencia.AddMonths(12)), Gestor, Comissao, inicioVigencia);
        return parceria;
    }

    [Fact]
    public void Fluxo_chamamento_termo_repasse_PC_OSC()
    {
        var parceria = CelebradaPorChamamento(out var inicio);
        parceria.Situacao.Should().Be(SituacaoParceriaOsc.Celebrada);
        parceria.Repasses.Should().HaveCount(2);

        // Repasse exige espelho orcamentario completo (empenho->liquidacao->pagamento — B-INV-5).
        parceria.VincularExecucaoOrcamentaria(1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        parceria.LiberarRepasse(1, inicio.AddMonths(1));

        parceria.Situacao.Should().Be(SituacaoParceriaOsc.EmExecucao);
        parceria.Repasses[0].Situacao.Should().Be(SituacaoRepasse.Liberada);

        // PC da OSC: entrega 90 d apos a vigencia, analise 150 d a partir do recebimento (prazos CALCULADOS).
        parceria.AbrirPrestacaoOsc(PrazoEntregaPc, Calendario);
        parceria.Situacao.Should().Be(SituacaoParceriaOsc.EmPrestacaoContas);
        parceria.Prestacao!.PrazoEntrega.Vencimento.Should().Be(parceria.Vigencia!.Fim.AddDays(90));

        var dataRecebimento = parceria.Vigencia.Fim.AddDays(30);
        parceria.ReceberPrestacaoOsc(dataRecebimento, PrazoAnalisePc, Calendario);
        parceria.Situacao.Should().Be(SituacaoParceriaOsc.EmAnalise);
        parceria.Prestacao.PrazoAnalise!.Vencimento.Should().Be(dataRecebimento.AddDays(150));

        parceria.IniciarAnalisePrestacao();
        parceria.ConcluirAnalisePrestacao(ResultadoAnalise.Aprovada);
        parceria.Situacao.Should().Be(SituacaoParceriaOsc.Aprovada);
    }

    [Fact]
    public void Inadimplencia_BLOQUEIA_novo_repasse_B_INV_9()
    {
        var parceria = CelebradaPorChamamento(out var inicio);

        // Libera a parcela 1 normalmente.
        parceria.VincularExecucaoOrcamentaria(1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        parceria.LiberarRepasse(1, inicio.AddMonths(1));

        // Mesmo com a parcela 2 com execucao orcamentaria pronta...
        parceria.VincularExecucaoOrcamentaria(2, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // ...declarada a INADIMPLENCIA (PC nao entregue / irregularidade — gatilho LRF art. 48):
        parceria.DeclararInadimplencia("PC da OSC nao entregue no prazo (art. 70)");

        parceria.Situacao.Should().Be(SituacaoParceriaOsc.Inadimplente);
        parceria.Repasses[1].Situacao.Should().Be(SituacaoRepasse.Bloqueada);

        // Qualquer nova liberacao e BARRADA.
        var act = () => parceria.LiberarRepasse(2, inicio.AddMonths(4));
        act.Should().Throw<InvalidParceriaStateException>().WithMessage("*inadimplente*");
    }

    [Fact]
    public void Repasse_ja_liberado_nao_e_bloqueado_pela_inadimplencia()
    {
        var parceria = CelebradaPorChamamento(out var inicio);
        parceria.VincularExecucaoOrcamentaria(1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        parceria.LiberarRepasse(1, inicio.AddMonths(1));

        parceria.DeclararInadimplencia("Irregularidade na execucao");

        // A parcela 1 ja liberada permanece Liberada; apenas as FUTURAS (previstas) sao bloqueadas.
        parceria.Repasses[0].Situacao.Should().Be(SituacaoRepasse.Liberada);
        parceria.Repasses[1].Situacao.Should().Be(SituacaoRepasse.Bloqueada);
    }

    [Fact]
    public void Repasse_sem_execucao_orcamentaria_completa_e_barrado_B_INV_5()
    {
        var parceria = CelebradaPorChamamento(out var inicio);

        // So empenho, sem liquidacao/pagamento -> nao libera (B-INV-5).
        parceria.VincularExecucaoOrcamentaria(1, Guid.NewGuid(), null, null);

        var act = () => parceria.LiberarRepasse(1, inicio.AddMonths(1));
        act.Should().Throw<InvalidOperationException>().WithMessage("*empenho*");
    }

    [Fact]
    public void Acordo_de_cooperacao_nao_admite_repasse_B_INV_2()
    {
        var inicio = new DateOnly(2026, 1, 1);
        var selecao = FormaSelecao.PorDispensa("art. 30 VI", "Acordo sem transferencia de recursos");
        var parceria = ParceriaOsc.IniciarSelecao(
            Tenant, OscHabilitada(inicio), TipoInstrumentoMrosc.AcordoCooperacao, selecao);

        // Acordo de Cooperacao: sem cronograma de repasse e sem valor global (B-INV-2).
        parceria.RegistrarPlanoTrabalho(
            "Cooperacao tecnica",
            Dinheiro.Zero,
            new[] { new MetaOsc("Cooperar", "Acoes", "5") },
            Array.Empty<ParcelaRepasse>());
        parceria.AprovarPlanoTrabalho();
        parceria.Celebrar(Vigencia.Criar(inicio, inicio.AddMonths(12)), Gestor, Comissao, inicio);

        parceria.PermiteRepasse.Should().BeFalse();
        parceria.Repasses.Should().BeEmpty();

        var act = () => parceria.LiberarRepasse(1, inicio.AddMonths(1));
        act.Should().Throw<InvalidParceriaStateException>().WithMessage("*Cooperacao*");
    }

    [Fact]
    public void Celebracao_exige_OSC_habilitada_B_INV_3()
    {
        var inicio = new DateOnly(2026, 1, 1);
        var oscSemCertidao = Osc.Criar(
            Cnpj.Create("11222333000181"), "Instituto", "Associacao", true, true, certidoes: null);
        var selecao = FormaSelecao.PorChamamento(Guid.NewGuid(), "Edital", homologado: true);
        var parceria = ParceriaOsc.IniciarSelecao(Tenant, oscSemCertidao, TipoInstrumentoMrosc.TermoColaboracao, selecao);
        parceria.RegistrarPlanoTrabalho(
            "Objeto",
            Dinheiro.De(10_000m),
            new[] { new MetaOsc("Meta", "Ind", "1") },
            new[] { new ParcelaRepasse(1, Dinheiro.De(10_000m), inicio.AddMonths(1), "") });
        parceria.AprovarPlanoTrabalho();

        // Fail-closed: sem certidoes a OSC NAO esta habilitada (B-INV-3).
        var act = () => parceria.Celebrar(Vigencia.Criar(inicio, inicio.AddMonths(12)), Gestor, Comissao, inicio);
        act.Should().Throw<InvalidParceriaStateException>().WithMessage("*habilitada*");
    }
}
