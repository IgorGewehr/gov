using FluentAssertions;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Normas;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// Bordas de proposicao/norma/diario: parecer contrario da CCJ (BUG-5), busca de norma com acento
/// (BUG-3), datas implausiveis e auto-referencia na trilha juridica da norma, e unicidade da decisao
/// do Executivo (sancao/veto).
/// </summary>
public sealed class BordasProposicaoNormaTests : LegislativoTestBase
{
    private static readonly DateOnly Hoje = new(2026, 6, 22);

    private static Proposicao NovaProposicao(TipoProposicao tipo = TipoProposicao.ProjetoDeLeiOrdinaria)
        => Proposicao.Apresentar(
            TenantA,
            tipo,
            Ementa.De("Dispoe sobre o servico publico municipal"),
            Autoria.De("Vereador Joao"),
            RegimeTramitacao.Ordinario,
            Hoje,
            "PROT-XYZ");

    private static Norma NovaNorma(int numero = 10, int ano = 2025)
        => Norma.Promulgar(
            TenantA,
            TipoNorma.Lei,
            numero,
            ano,
            Ementa.De("Dispoe sobre o calendario de eventos do municipio."),
            new DateOnly(ano, 3, 10));

    // ---------- BUG-5: parecer CONTRARIO da CCJ ----------

    [Fact] // Item 11: parecer CCJ contrario (inconstitucionalidade) bloqueia a Ordem do Dia.
    public void IncluirEmOrdemDoDia_com_parecer_ccj_contrario_falha()
    {
        var proposicao = NovaProposicao();
        proposicao.Distribuir(Hoje);
        proposicao.RegistrarParecer(Proposicao.ComissaoCcj, favoravel: false, Hoje); // CONTRARIO
        proposicao.RegistrarParecer(Proposicao.ComissaoFinancasOrcamento, favoravel: true, Hoje);

        ((Action)(() => proposicao.IncluirEmOrdemDoDia(Hoje)))
            .Should().Throw<InvalidOperationException>().WithMessage("*inconstitucionalidade*");
        proposicao.Situacao.Should().Be(SituacaoProposicao.Distribuida);
    }

    [Fact] // BUG-5: o parecer contrario da CCJ pode ser SUPERADO pelo Plenario -> libera a Ordem do Dia.
    public void IncluirEmOrdemDoDia_com_parecer_ccj_contrario_superado_passa()
    {
        var proposicao = NovaProposicao();
        proposicao.Distribuir(Hoje);
        proposicao.RegistrarParecer(Proposicao.ComissaoCcj, favoravel: false, Hoje);
        proposicao.RegistrarParecer(Proposicao.ComissaoFinancasOrcamento, favoravel: true, Hoje);

        proposicao.SuperarParecerContrarioCcj();
        proposicao.IncluirEmOrdemDoDia(Hoje);

        proposicao.Situacao.Should().Be(SituacaoProposicao.EmOrdemDoDia);
    }

    [Fact] // BUG-5: parecer CCJ favoravel continua passando normalmente (nao regrediu).
    public void IncluirEmOrdemDoDia_com_parecer_ccj_favoravel_passa()
    {
        var proposicao = NovaProposicao();
        proposicao.Distribuir(Hoje);
        proposicao.RegistrarParecer(Proposicao.ComissaoCcj, favoravel: true, Hoje);
        proposicao.RegistrarParecer(Proposicao.ComissaoFinancasOrcamento, favoravel: true, Hoje);

        proposicao.IncluirEmOrdemDoDia(Hoje);
        proposicao.Situacao.Should().Be(SituacaoProposicao.EmOrdemDoDia);
    }

    [Fact] // BUG-5: superar sem haver parecer contrario da CCJ e invalido.
    public void SuperarParecerContrarioCcj_sem_parecer_contrario_lanca()
    {
        var proposicao = NovaProposicao();
        proposicao.Distribuir(Hoje);
        proposicao.RegistrarParecer(Proposicao.ComissaoCcj, favoravel: true, Hoje);

        ((Action)proposicao.SuperarParecerContrarioCcj).Should().Throw<InvalidOperationException>();
    }

    // ---------- Item 15: sancao/veto sao decisao UNICA do Executivo ----------

    [Fact] // RegistrarSancao duas vezes: a segunda e recusada (decisao final e unica).
    public void RegistrarSancao_duas_vezes_recusa_a_segunda()
    {
        var proposicao = ProposicaoEmAutografo();

        proposicao.RegistrarSancao(Hoje).Should().BeTrue();
        proposicao.RegistrarSancao(Hoje).Should().BeFalse();
    }

    [Fact] // RegistrarVeto apos sancao: recusado ("Sancao, Veto, Sancao" nao e aceito).
    public void RegistrarVeto_apos_sancao_recusa()
    {
        var proposicao = ProposicaoEmAutografo();

        proposicao.RegistrarSancao(Hoje).Should().BeTrue();
        proposicao.RegistrarVeto(Hoje).Should().BeFalse();
        proposicao.RegistrarSancao(Hoje).Should().BeFalse();
    }

    private static Proposicao ProposicaoEmAutografo()
    {
        var proposicao = NovaProposicao();
        proposicao.Distribuir(Hoje);
        proposicao.RegistrarParecer(Proposicao.ComissaoCcj, favoravel: true, Hoje);
        proposicao.RegistrarParecer(Proposicao.ComissaoFinancasOrcamento, favoravel: true, Hoje);
        proposicao.IncluirEmOrdemDoDia(Hoje);
        proposicao.Aprovar(ResultadoDeliberacao.Aprovada(MaioriaProposicao.Simples), Hoje);
        proposicao.GerarAutografo("AUT-2026-0001", Hoje);
        return proposicao;
    }

    // ---------- Itens 13/14: trilha juridica da norma ----------

    [Fact] // Item 13: alteracao com data anterior a promulgacao e rejeitada.
    public void RegistrarAlteracao_com_data_anterior_a_promulgacao_lanca()
    {
        var norma = NovaNorma(ano: 2025);

        ((Action)(() => norma.RegistrarAlteracao(new DateOnly(2024, 1, 1), NormaId.New())))
            .Should().Throw<ArgumentException>();
    }

    [Fact] // Item 14: revogar com norma revogadora igual a si mesma (auto-referencia) e rejeitado.
    public void Revogar_com_norma_revogadora_igual_a_si_mesma_lanca()
    {
        var norma = NovaNorma();

        ((Action)(() => norma.Revogar(new DateOnly(2026, 1, 1), norma.Id)))
            .Should().Throw<ArgumentException>();
    }

    [Fact] // Item 14 (espelho): alterar com norma alteradora igual a si mesma e rejeitado.
    public void RegistrarAlteracao_com_norma_alteradora_igual_a_si_mesma_lanca()
    {
        var norma = NovaNorma();

        ((Action)(() => norma.RegistrarAlteracao(new DateOnly(2026, 1, 1), norma.Id)))
            .Should().Throw<ArgumentException>();
    }

    // ---------- BUG-3: busca de norma com acento ----------

    [Fact] // Item 12 (FALHAVA antes): termo "acacias" sem acento casa "Acácias" na ementa.
    public async Task Buscar_por_termo_com_acento_diferente_casa()
    {
        await using (var contexto = CriarContexto(TenantA))
        {
            contexto.Normas.Add(Norma.Promulgar(
                TenantA, TipoNorma.Lei, 200, 2026,
                Ementa.De("Denomina a Rua das Acácias e a Praça São João no Município."),
                new DateOnly(2026, 6, 10)));
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var repo = new NormaRepository(contexto);

            var (acacias, totalAcacias) = await repo.BuscarAsync(
                new FiltroNormas("acacias", null, null, null, null, 1, 20), CancellationToken.None);
            totalAcacias.Should().Be(1);
            acacias.Should().ContainSingle();

            // "sao joao" (sem acento/cedilha) casa "São João".
            var (saoJoao, totalSaoJoao) = await repo.BuscarAsync(
                new FiltroNormas("sao joao", null, null, null, null, 1, 20), CancellationToken.None);
            totalSaoJoao.Should().Be(1);
            saoJoao.Should().ContainSingle();
        }
    }

    [Fact] // BUG-3: o normalizador remove diacriticos e baixa a caixa de forma simetrica.
    public void Normalizar_remove_acentos_e_cedilha_e_baixa_caixa()
    {
        TextoBusca.Normalizar("São João das Acácias").Should().Be("sao joao das acacias");
        TextoBusca.Normalizar("ACÓRDÃO").Should().Be("acordao");
        TextoBusca.Normalizar(null).Should().BeEmpty();
    }
}
