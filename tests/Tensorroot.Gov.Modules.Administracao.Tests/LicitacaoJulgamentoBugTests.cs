using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Administracao.Tests;

/// <summary>
/// Cobertura de borda dos bugs do agregado <see cref="Licitacao"/> (integridade licitatoria, TCE):
/// BUG-A3 (enforcement de menor preco/lote no julgamento) e BUG-A4 (habilitacao mais recente,
/// determinismo apos reidratacao do EF Core), alem da semantica de fracasso (L-A3).
/// </summary>
public sealed class LicitacaoJulgamentoBugTests : AdministracaoTestBase
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 21, 9, 0, 0, TimeSpan.Zero);

    private static Licitacao NovoPregaoMenorPreco()
        => Licitacao.Abrir(
            TenantA,
            "Aquisicao de notebooks",
            ModalidadeLicitacao.Pregao,
            CriterioJulgamento.MenorPreco,
            ValorMonetario.De(200000m));

    // ---------- BUG-A3: enforcement de menor preco e lote ----------

    [Fact] // BUG-A3: indicar vencedora que NAO e a de menor preco (mesmo lote) e rejeitado.
    public void BugA3_indicar_vencedora_que_nao_e_menor_preco_e_rejeitado()
    {
        var lic = NovoPregaoMenorPreco();
        var lote = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        lic.RegistrarProposta(Guid.NewGuid(), lote, ValorMonetario.De(90000m));
        var caraId = lic.RegistrarProposta(Guid.NewGuid(), lote, ValorMonetario.De(100000m));
        lic.RegistrarProposta(Guid.NewGuid(), lote, ValorMonetario.De(80000m));

        var acao = () => lic.JulgarPropostas(caraId.Value);

        acao.Should().Throw<InvalidOperationException>();
        lic.Situacao.Should().Be(SituacaoLicitacao.Aberta);
    }

    [Fact] // BUG-A3: indicar a de menor preco do lote e aceito.
    public void BugA3_indicar_menor_preco_e_aceito()
    {
        var lic = NovoPregaoMenorPreco();
        var lote = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        lic.RegistrarProposta(Guid.NewGuid(), lote, ValorMonetario.De(90000m));
        var baratoId = lic.RegistrarProposta(Guid.NewGuid(), lote, ValorMonetario.De(80000m));

        lic.JulgarPropostas(baratoId.Value);

        lic.Situacao.Should().Be(SituacaoLicitacao.EmJulgamento);
        lic.ValorAdjudicado().Should().Be(80000m);
    }

    [Fact] // BUG-A3: a melhor proposta desclassificada nao impede indicar a melhor das remanescentes.
    public void BugA3_desclassificada_nao_conta_na_disputa()
    {
        var lic = NovoPregaoMenorPreco();
        var lote = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        var baratoDesclassificadoId = lic.RegistrarProposta(Guid.NewGuid(), lote, ValorMonetario.De(70000m));
        var melhorValidaId = lic.RegistrarProposta(Guid.NewGuid(), lote, ValorMonetario.De(80000m));
        lic.RegistrarProposta(Guid.NewGuid(), lote, ValorMonetario.De(90000m));

        lic.Propostas.Single(p => p.Id == baratoDesclassificadoId).Desclassificar();
        lic.JulgarPropostas(melhorValidaId.Value);

        lic.ValorAdjudicado().Should().Be(80000m);
    }

    [Fact] // BUG-A3: o enforcement e por LOTE — a vencedora de um lote nao concorre com outro lote.
    public void BugA3_enforcement_e_por_lote()
    {
        var lic = NovoPregaoMenorPreco();
        var lote1 = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        var lote2 = lic.AdicionarLote(2, "Lote 2", ValorMonetario.De(100000m));
        lic.RegistrarProposta(Guid.NewGuid(), lote1, ValorMonetario.De(50000m)); // mais barata, lote 1.
        var melhorLote2Id = lic.RegistrarProposta(Guid.NewGuid(), lote2, ValorMonetario.De(80000m)); // melhor do lote 2.
        lic.RegistrarProposta(Guid.NewGuid(), lote2, ValorMonetario.De(90000m));

        // Indicar a melhor do lote 2 nao deve ser barrada pela existencia de uma mais barata no lote 1.
        lic.JulgarPropostas(melhorLote2Id.Value);

        lic.ValorAdjudicado().Should().Be(80000m);
    }

    // ---------- BUG-A4: prevalece a habilitacao mais recente ----------

    [Fact] // BUG-A4: Inabilitado -> Habilitado (mais recente) permite homologar.
    public void BugA4_habilitacao_mais_recente_habilitado_permite_homologar()
    {
        var lic = NovoPregaoMenorPreco();
        var lote = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        var fornecedor = Guid.NewGuid();
        var prop = lic.RegistrarProposta(fornecedor, lote, ValorMonetario.De(90000m));
        lic.JulgarPropostas(prop.Value);

        lic.HabilitarLicitante(fornecedor, ResultadoHabilitacao.Inabilitado, "Doc pendente", T0, fornecedorImpedido: false);
        lic.HabilitarLicitante(fornecedor, ResultadoHabilitacao.Habilitado, "Recurso provido", T0.AddHours(1), fornecedorImpedido: false);

        lic.Homologar(fornecedorVencedorImpedido: false);
        lic.Situacao.Should().Be(SituacaoLicitacao.Homologada);
    }

    [Fact] // BUG-A4: Habilitado -> Inabilitado (mais recente) BLOQUEIA homologacao.
    public void BugA4_habilitacao_mais_recente_inabilitado_bloqueia_homologacao()
    {
        var lic = NovoPregaoMenorPreco();
        var lote = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        var fornecedor = Guid.NewGuid();
        var prop = lic.RegistrarProposta(fornecedor, lote, ValorMonetario.De(90000m));
        lic.JulgarPropostas(prop.Value);

        lic.HabilitarLicitante(fornecedor, ResultadoHabilitacao.Habilitado, null, T0, fornecedorImpedido: false);
        lic.HabilitarLicitante(fornecedor, ResultadoHabilitacao.Inabilitado, "Doc vencido", T0.AddHours(1), fornecedorImpedido: false);

        ((Action)(() => lic.Homologar(fornecedorVencedorImpedido: false))).Should().Throw<InvalidOperationException>();
        lic.Situacao.Should().Be(SituacaoLicitacao.EmJulgamento);
    }

    [Fact] // BUG-A4: a decisao e a MESMA antes e depois do SaveChanges (round-trip de persistencia).
    public async Task BugA4_decisao_estavel_apos_round_trip_de_persistencia()
    {
        LicitacaoId id;
        Guid fornecedor = Guid.NewGuid();

        await using (var contexto = CriarContexto(TenantA))
        {
            var lic = NovoPregaoMenorPreco();
            var lote = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
            var prop = lic.RegistrarProposta(fornecedor, lote, ValorMonetario.De(90000m));
            lic.JulgarPropostas(prop.Value);
            // Ordem de insercao: Habilitado e depois Inabilitado (mais recente) — deve prevalecer Inabilitado.
            lic.HabilitarLicitante(fornecedor, ResultadoHabilitacao.Habilitado, null, T0, fornecedorImpedido: false);
            lic.HabilitarLicitante(fornecedor, ResultadoHabilitacao.Inabilitado, "Doc vencido", T0.AddHours(1), fornecedorImpedido: false);
            id = lic.Id;
            contexto.Licitacoes.Add(lic);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = CriarContexto(TenantA))
        {
            var lic = await contexto.Licitacoes.SingleAsync(l => l.Id == id);

            // Apos reidratacao do EF Core, a habilitacao vigente continua sendo Inabilitado -> bloqueia.
            ((Action)(() => lic.Homologar(fornecedorVencedorImpedido: false))).Should().Throw<InvalidOperationException>();
            lic.Habilitacoes.Should().HaveCount(2);
        }
    }

    [Fact] // L-A3: proposta ainda sem habilitacao conta como potencialmente valida e bloqueia o fracasso.
    public void LA3_proposta_pendente_de_habilitacao_bloqueia_fracasso()
    {
        var lic = NovoPregaoMenorPreco();
        var lote = lic.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        lic.RegistrarProposta(Guid.NewGuid(), lote, ValorMonetario.De(90000m));
        // Ninguem habilitado/inabilitado ainda.

        ((Action)(() => lic.DeclararFracassada("Sem habilitados"))).Should().Throw<InvalidOperationException>();
    }
}
