using FluentAssertions;
using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.Modules.Convenios.Domain.Parametros;
using Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;
using Tensorroot.Gov.SharedKernel.Tempo;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Convenios.Tests;

/// <summary>
/// Testes do agregado <see cref="ConvenioRecebido"/> (fluxo A — convenios federais recebidos, Dec.
/// 11.531/2023): prazo de analise da PC parcial (60 d) calculado e VENCIDO, saneamento (45 d) com decisao
/// habilitada apos vencimento, e VINCULO ORCAMENTARIO (empenho da contrapartida -&gt; A-INV-5 destrava a
/// execucao). Prazos sempre CALCULADOS via calendario do tenant — nunca digitados (CLAUDE.md S7/S16).
/// </summary>
public sealed class ConvenioRecebidoTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly ICalendarioDiasUteis Calendario = new CalendarioCorridoFake();

    // Prazos do tenant (defaults da norma-fonte — Dec. 11.531/2023). NUNCA hardcoded no dominio: chegam por parametro.
    private static readonly ParametroPrazo PrazoParcial = new(60, UnidadePrazo.DiasCorridos, "Dec. 11.531/2023 (parcial)");
    private static readonly ParametroPrazo PrazoFinal = new(180, UnidadePrazo.DiasCorridos, "Dec. 11.531/2023 (final)");
    private static readonly ParametroPrazo PrazoSaneamento = new(45, UnidadePrazo.DiasCorridos, "Portaria Conjunta 33/2023");

    private static ConvenioRecebido CelebradoEmExecucao()
    {
        var concedente = OrgaoConcedente.Criar(
            Cnpj.Create("11222333000181"),
            "Ministerio do Desenvolvimento",
            EsferaConcedente.Uniao,
            SistemaOrigemConvenio.Transferegov);

        var repasse = Dinheiro.De(100_000m);
        var contrapartidaValor = Dinheiro.De(20_000m);
        var inicio = new DateOnly(2026, 1, 1);

        var etapas = new[]
        {
            new EtapaPlanoTrabalho(1, "Execucao do objeto", Dinheiro.De(120_000m), inicio, inicio.AddMonths(6)),
        };
        var parcelas = new[]
        {
            new ParcelaPrevista(1, Dinheiro.De(60_000m), inicio.AddMonths(1)),
            new ParcelaPrevista(2, Dinheiro.De(40_000m), inicio.AddMonths(3)),
        };

        var plano = PlanoDeTrabalho.Criar("Objeto", repasse, contrapartidaValor, etapas, parcelas);
        var convenio = ConvenioRecebido.RegistrarProposta(Tenant, concedente, plano);
        convenio.AprovarPlanoTrabalho();

        var contrapartida = Contrapartida.Criar(
            ModalidadeContrapartida.Financeira,
            contrapartidaValor,
            percentualMinimo: 20m,
            normaFontePercentual: "Portaria Conjunta 33/2023");

        convenio.Celebrar(Vigencia.Criar(inicio, inicio.AddMonths(12)), contrapartida, "TV-2026/0001");

        // VINCULO ORCAMENTARIO: empenho da contrapartida (A-INV-5) destrava a execucao.
        convenio.RegistrarContrapartidaEmpenhada(contrapartidaValor);
        return convenio;
    }

    [Fact]
    public void Empenho_total_da_contrapartida_destrava_execucao_A_INV_5()
    {
        var convenio = CelebradoEmExecucao();
        convenio.Situacao.Should().Be(SituacaoConvenioRecebido.EmExecucao);
        convenio.Contrapartida!.TotalmenteEmpenhada.Should().BeTrue();
    }

    [Fact]
    public void Sem_empenho_total_da_contrapartida_nao_ha_execucao_A_INV_5()
    {
        var concedente = OrgaoConcedente.Criar(
            Cnpj.Create("11222333000181"), "Concedente", EsferaConcedente.Uniao, SistemaOrigemConvenio.Transferegov);
        var inicio = new DateOnly(2026, 1, 1);
        var plano = PlanoDeTrabalho.Criar(
            "Objeto",
            Dinheiro.De(100_000m),
            Dinheiro.De(20_000m),
            new[] { new EtapaPlanoTrabalho(1, "Etapa", Dinheiro.De(120_000m), inicio, inicio.AddMonths(6)) },
            new[] { new ParcelaPrevista(1, Dinheiro.De(100_000m), inicio.AddMonths(1)) });
        var convenio = ConvenioRecebido.RegistrarProposta(Tenant, concedente, plano);
        convenio.AprovarPlanoTrabalho();
        convenio.Celebrar(
            Vigencia.Criar(inicio, inicio.AddMonths(12)),
            Contrapartida.Criar(ModalidadeContrapartida.Financeira, Dinheiro.De(20_000m), 20m, "Portaria 33/2023"),
            "TV-2026/0002");

        // Empenho PARCIAL: nao atinge o pactuado -> permanece Celebrado (sem execucao).
        convenio.RegistrarContrapartidaEmpenhada(Dinheiro.De(5_000m));

        convenio.Situacao.Should().Be(SituacaoConvenioRecebido.Celebrado);
        convenio.Contrapartida!.TotalmenteEmpenhada.Should().BeFalse();
    }

    [Fact]
    public void Inadimplente_bloqueia_liberacao_de_parcela_A_INV_10()
    {
        var convenio = CelebradoEmExecucao();
        convenio.DeclararInadimplencia("PC final rejeitada pelo concedente");

        var act = () => convenio.RegistrarLiberacaoParcela(1, new DateOnly(2026, 2, 1));

        act.Should().Throw<InvalidConvenioStateException>().WithMessage("*inadimplente*");
    }

    [Fact]
    public void Liberacao_da_parcela_2_exige_PC_parcial_da_etapa_1_submetida_por_NUMERO_estruturado_A_INV_4()
    {
        var convenio = CelebradoEmExecucao();

        // PC parcial da etapa 1 com competencia em texto livre que NAO contem o digito "1" (ex.: falso-negativo
        // do bug antigo por substring). Com vinculo estruturado, a etapa 1 e reconhecida e a parcela 2 libera.
        var pc = convenio.AbrirPrestacaoParcial(1, "Primeira competencia");
        convenio.SubmeterPrestacao(pc.Id, new DateOnly(2026, 2, 10), PrazoParcial, PrazoFinal, Calendario);

        var act = () => convenio.RegistrarLiberacaoParcela(2, new DateOnly(2026, 4, 1));

        act.Should().NotThrow();
    }

    [Fact]
    public void Liberacao_da_parcela_2_SEM_PC_da_etapa_1_e_bloqueada_mesmo_com_digito_em_outra_PC_A_INV_4()
    {
        var convenio = CelebradoEmExecucao();

        // PC parcial da PROPRIA parcela 2 (texto livre "2026/01" — o digito "1" disparava o falso-positivo do
        // bug antigo por substring). Como NAO existe PC da etapa 1, a parcela 2 NAO pode ser liberada.
        var pc = convenio.AbrirPrestacaoParcial(2, "2026/01");
        convenio.SubmeterPrestacao(pc.Id, new DateOnly(2026, 2, 10), PrazoParcial, PrazoFinal, Calendario);

        var act = () => convenio.RegistrarLiberacaoParcela(2, new DateOnly(2026, 4, 1));

        act.Should().Throw<InvalidConvenioStateException>().WithMessage("*etapa anterior*");
    }

    [Fact]
    public void PC_parcial_de_etapa_inexistente_no_cronograma_e_rejeitada()
    {
        var convenio = CelebradoEmExecucao();

        var act = () => convenio.AbrirPrestacaoParcial(99, "Etapa 99");

        act.Should().Throw<InvalidConvenioStateException>().WithMessage("*nao prevista no cronograma*");
    }

    [Fact]
    public void PC_parcial_submetida_tem_prazo_de_analise_calculado_e_fica_VENCIDO_apos_60_dias()
    {
        var convenio = CelebradoEmExecucao();
        var dataSubmissao = new DateOnly(2026, 2, 10);

        var pc = convenio.AbrirPrestacaoParcial(1, "Etapa 1");
        convenio.SubmeterPrestacao(pc.Id, dataSubmissao, PrazoParcial, PrazoFinal, Calendario);

        // Prazo CALCULADO: 60 dias corridos a partir da submissao.
        var vencimentoEsperado = dataSubmissao.AddDays(60);
        pc.PrazoAnalise.Should().NotBeNull();
        pc.PrazoAnalise!.Vencimento.Should().Be(vencimentoEsperado);

        // No dia seguinte ao vencimento, sem decisao, o prazo esta VENCIDO.
        pc.PrazoAnalise.Vencido(vencimentoEsperado.AddDays(1)).Should().BeTrue();
        pc.PrazoAnalise.AVencer(vencimentoEsperado).Should().BeTrue();

        // A PC PARCIAL e um ato CONTINUO da execucao: a analise corre na sub-maquina da PROPRIA PC (Submetida),
        // mas o AGREGADO permanece EmExecucao — so a PC FINAL encerra a execucao e leva o convenio a EmAnalise.
        pc.Situacao.Should().Be(SituacaoPrestacaoConvenio.Submetida);
        convenio.Situacao.Should().Be(SituacaoConvenioRecebido.EmExecucao);
    }

    [Fact]
    public void Saneamento_concede_prazo_45_dias_e_habilita_rejeicao_apos_vencimento_A_INV_9()
    {
        var convenio = CelebradoEmExecucao();
        var pc = convenio.AbrirPrestacaoParcial(1, "Etapa 1");
        convenio.SubmeterPrestacao(pc.Id, new DateOnly(2026, 2, 10), PrazoParcial, PrazoFinal, Calendario);
        convenio.IniciarAnalisePrestacao(pc.Id);

        var dataNotificacao = new DateOnly(2026, 3, 1);
        convenio.AbrirSaneamentoPrestacao(pc.Id, dataNotificacao, PrazoSaneamento, Calendario);

        // Saneamento de 45 dias corridos a partir da notificacao da pendencia.
        var vencimentoSaneamento = dataNotificacao.AddDays(45);
        pc.PrazoSaneamento.Should().NotBeNull();
        pc.PrazoSaneamento!.Vencimento.Should().Be(vencimentoSaneamento);
        pc.SaneamentoConcedido.Should().BeTrue();
        pc.PrazoSaneamento.Vencido(vencimentoSaneamento.AddDays(1)).Should().BeTrue();

        // Nao saneou: a analise e retomada e a PC pode ser REJEITADA (PC parcial -> volta a execucao).
        convenio.RetomarAnalisePrestacao(pc.Id);
        convenio.ConcluirAnalisePrestacao(pc.Id, ResultadoAnalise.Rejeitada);
        pc.Resultado.Should().Be(ResultadoAnalise.Rejeitada);
    }

    [Fact]
    public void Saneamento_so_pode_ser_concedido_uma_vez_A_INV_9()
    {
        var convenio = CelebradoEmExecucao();
        var pc = convenio.AbrirPrestacaoParcial(1, "Etapa 1");
        convenio.SubmeterPrestacao(pc.Id, new DateOnly(2026, 2, 10), PrazoParcial, PrazoFinal, Calendario);
        convenio.IniciarAnalisePrestacao(pc.Id);
        convenio.AbrirSaneamentoPrestacao(pc.Id, new DateOnly(2026, 3, 1), PrazoSaneamento, Calendario);

        var act = () => convenio.AbrirSaneamentoPrestacao(pc.Id, new DateOnly(2026, 4, 1), PrazoSaneamento, Calendario);

        act.Should().Throw<Exception>();
    }
}
