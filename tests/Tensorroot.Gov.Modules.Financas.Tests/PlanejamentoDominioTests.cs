using FluentAssertions;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Creditos;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Invariantes de domínio do planejamento orçamentário (PPA/LDO/LOA + créditos):
/// cadeia de compatibilidade LOA ⊆ LDO ⊆ PPA, máquinas de estado, equilíbrio e crédito que altera a LOA.
/// </summary>
public sealed class PlanejamentoDominioTests
{
    private static readonly Guid Tenant = Guid.Parse("77777777-7777-7777-7777-777777777777");

    private static ClassificacaoOrcamentaria Classificacao()
        => ClassificacaoOrcamentaria.De("02", "0201", "04.122.0002.2010", CategoriaEconomica.DespesasCorrentes, "0001");

    private static NaturezaReceita NaturezaReceitaPadrao()
        => NaturezaReceita.De(CategoriaEconomicaReceita.ReceitasCorrentes, "1", "1.1", "1.1.1.8.01");

    // ----- PPA: máquina de estados e quadriênio -----

    [Fact]
    public void Ppa_quadrienio_tem_quatro_anos()
    {
        var ppa = PlanoPlurianual.Criar(Tenant, 2026, "PPA 01/2025", 2025);
        ppa.AnoInicio.Should().Be(2026);
        ppa.AnoFim.Should().Be(2029);
    }

    [Fact]
    public void Ppa_so_e_referenciavel_quando_vigente()
    {
        var ppa = PpaComAcaoVigente(out var acaoId);
        ppa.ContemAcaoVigente(acaoId).Should().BeTrue();

        var emElaboracao = PlanoPlurianual.Criar(Tenant, 2026, "PPA 02/2025", 2025);
        emElaboracao.ContemAcaoVigente(acaoId).Should().BeFalse();
    }

    [Fact]
    public void Ppa_nao_tramita_sem_acao_com_meta()
    {
        var ppa = PlanoPlurianual.Criar(Tenant, 2026, "PPA 03/2025", 2025);
        ppa.AdicionarPrograma("0002", "Educacao", "Obj", "Alunos", "IDEB", 4.0m, 5.0m);

        var acao = () => ppa.ColocarEmTramitacao();

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Ppa_em_vigor_nao_aceita_edicao()
    {
        var ppa = PpaComAcaoVigente(out _);
        var acao = () => ppa.AdicionarPrograma("0003", "Saude", "Obj", "Pop", "Cob", 1m, 2m);

        acao.Should().Throw<TransicaoPlanejamentoInvalidaException>();
    }

    // ----- LDO: elo e prioridade só de ação vigente do PPA -----

    [Fact]
    public void Ldo_so_prioriza_acao_vigente_do_ppa()
    {
        var ppa = PpaComAcaoVigente(out var acaoId);
        var ldo = LeiDiretrizes.Criar(Tenant, 2026, ppa, "LDO 01/2025", 2025);

        ldo.PriorizarAcao(ppa, acaoId, 1, "prioridade do exercicio");
        ldo.DefinirMetaFiscal(2026, ValorMonetario.De(1000m), ValorMonetario.De(1000m), 0m, 0m, ValorMonetario.De(0m));
        ldo.ColocarEmTramitacao();
        ldo.Vigorar("LDO 01/2025", 2025, exigirAmf: false, exigirArf: false);

        ldo.ContemPrioridade(acaoId).Should().BeTrue();
    }

    [Fact]
    public void Ldo_recusa_prioridade_de_acao_inexistente()
    {
        var ppa = PpaComAcaoVigente(out _);
        var ldo = LeiDiretrizes.Criar(Tenant, 2026, ppa, "LDO 02/2025", 2025);

        var acao = () => ldo.PriorizarAcao(ppa, AcaoPpaId.New(), 1, "acao fantasma");

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Ldo_exige_anexo_amf_quando_obrigatorio()
    {
        var ppa = PpaComAcaoVigente(out var acaoId);
        var ldo = LeiDiretrizes.Criar(Tenant, 2026, ppa, "LDO 03/2025", 2025);
        ldo.PriorizarAcao(ppa, acaoId, 1, "x");
        ldo.DefinirMetaFiscal(2026, ValorMonetario.De(1m), ValorMonetario.De(1m), 0m, 0m, ValorMonetario.De(0m));
        ldo.ColocarEmTramitacao();

        var acao = () => ldo.Vigorar("LDO 03/2025", 2025, exigirAmf: true, exigirArf: false);

        acao.Should().Throw<InvalidOperationException>();
    }

    // ----- LOA: equilíbrio e dotação nasce do item -----

    [Fact]
    public void Loa_em_execucao_levanta_evento_por_item_sem_dotacao()
    {
        var (loa, _) = LoaAprovada(out var itemId);
        loa.EntrarEmExecucao();

        loa.DomainEvents.OfType<Domain.Planejamento.Events.ItemLoaEntrouEmExecucao>()
            .Should().ContainSingle(e => e.ItemId == itemId);
    }

    [Fact]
    public void Loa_registra_dotacao_gerada_fecha_elo_e_nao_reemite()
    {
        var (loa, _) = LoaAprovada(out var itemId);
        loa.EntrarEmExecucao();
        loa.ClearDomainEvents();

        loa.RegistrarDotacaoGerada(itemId, DotacaoOrcamentariaId.New());

        loa.EntrarEmExecucao(); // idempotente: item ja vinculado nao reemite
        loa.DomainEvents.OfType<Domain.Planejamento.Events.ItemLoaEntrouEmExecucao>().Should().BeEmpty();
        loa.Itens.Single(i => i.Id == itemId).DotacaoGerada.Should().BeTrue();
    }

    [Fact]
    public void Dotacao_de_loa_nasce_com_origem_rastreavel()
    {
        var loaId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var acaoId = Guid.NewGuid();

        var dotacao = DotacaoOrcamentaria.CriarDeLoa(Tenant, 2026, Classificacao(), ValorMonetario.De(5000m), loaId, itemId, acaoId);

        dotacao.ValorDotadoInicial.Valor.Should().Be(5000m);
        dotacao.LoaId.Should().Be(loaId);
        dotacao.ItemDespesaFixadaId.Should().Be(itemId);
        dotacao.AcaoPpaId.Should().Be(acaoId);
        dotacao.Situacao.Should().Be(SituacaoDotacao.Ativa);
    }

    // ----- Crédito adicional altera a LOA (suplementar reforça dotação) -----

    [Fact]
    public void Credito_suplementar_por_anulacao_move_saldo_entre_dotacoes()
    {
        var alvo = DotacaoOrcamentaria.Criar(Tenant, 2026, Classificacao(), ValorMonetario.De(1000m));
        var origem = DotacaoOrcamentaria.Criar(Tenant, 2026, Classificacao(), ValorMonetario.De(1000m));
        var (loa, _) = LoaAprovada(out _);
        loa.EntrarEmExecucao();

        var credito = CreditoAdicional.Suplementar(
            Tenant, loa, ValorMonetario.De(300m), FonteRecursoCredito.AnulacaoDotacao,
            alvo.Id, origem.Id, "Art. 7º LOA", "Decreto 10/2026", porDecreto: true);

        // Efeito que o handler aplica: anula a origem e reforca o alvo.
        origem.AnularCredito(ValorMonetario.De(300m));
        alvo.Reforcar(ValorMonetario.De(300m));
        credito.Abrir();

        alvo.ValorAtualizado.Valor.Should().Be(1300m);
        origem.ValorAtualizado.Valor.Should().Be(700m);
        credito.Situacao.Should().Be(SituacaoCredito.Aberto);
    }

    [Fact]
    public void Credito_exige_loa_em_execucao()
    {
        var (loa, _) = LoaAprovada(out _); // ainda Aprovada, nao EmExecucao
        var alvo = DotacaoOrcamentaria.Criar(Tenant, 2026, Classificacao(), ValorMonetario.De(1000m));

        var acao = () => CreditoAdicional.Suplementar(
            Tenant, loa, ValorMonetario.De(100m), FonteRecursoCredito.ExcessoArrecadacao,
            alvo.Id, null, "Art. 7º", "Decreto 1", porDecreto: true);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Credito_extraordinario_dispensa_autorizador_e_fonte()
    {
        var (loa, _) = LoaAprovada(out _);
        loa.EntrarEmExecucao();

        var credito = CreditoAdicional.Extraordinario(Tenant, loa, ValorMonetario.De(2000m), "MP 5/2026");

        credito.Especie.Should().Be(EspecieCredito.Extraordinario);
        credito.Fonte.Should().Be(FonteRecursoCredito.Dispensada);
    }

    // ----- helpers -----

    private static PlanoPlurianual PpaComAcaoVigente(out AcaoPpaId acaoId)
    {
        var ppa = PlanoPlurianual.Criar(Tenant, 2026, "PPA 99/2025", 2025);
        var programaId = ppa.AdicionarPrograma("0002", "Educacao", "Obj", "Alunos", "IDEB", 4.0m, 5.0m);
        acaoId = ppa.AdicionarAcao(programaId, "2010", "Manutencao", TipoAcao.Atividade, "12.361.0002.2010", "Aluno atendido", "unidade");
        ppa.DefinirMeta(acaoId, 2026, 100m, "unidade", ValorMonetario.De(50000m), "Sede");
        ppa.ColocarEmTramitacao();
        ppa.Vigorar("PPA 99/2025", 2025);
        return ppa;
    }

    private static (LeiOrcamentariaAnual Loa, AcaoPpaId AcaoId) LoaAprovada(out ItemDespesaFixadaId itemId)
    {
        var ppa = PpaComAcaoVigente(out var acaoId);
        var ldo = LeiDiretrizes.Criar(Tenant, 2026, ppa, "LDO 99/2025", 2025);
        ldo.PriorizarAcao(ppa, acaoId, 1, "prioridade");
        ldo.DefinirMetaFiscal(2026, ValorMonetario.De(100000m), ValorMonetario.De(100000m), 0m, 0m, ValorMonetario.De(0m));
        ldo.ColocarEmTramitacao();
        ldo.Vigorar("LDO 99/2025", 2025, exigirAmf: false, exigirArf: false);

        var loa = LeiOrcamentariaAnual.Criar(Tenant, 2026, ldo, 25m, "LOA 99/2025", 2025);
        loa.PreverReceita(NaturezaReceitaPadrao(), "0001", ValorMonetario.De(50000m));
        itemId = loa.FixarDespesa(Classificacao(), acaoId, "3.3.90.30", ValorMonetario.De(50000m));

        var compat = new CompatibilidadeFake(true);
        loa.AprovarAsync(compat, CancellationToken.None).GetAwaiter().GetResult();
        return (loa, acaoId);
    }

    private sealed class CompatibilidadeFake(bool compativel) : ICompatibilidadeOrcamentariaService
    {
        public Task<ResultadoCompatibilidade> VerificarAsync(LeiOrcamentariaAnual loa, CancellationToken cancellationToken)
            => Task.FromResult(compativel ? ResultadoCompatibilidade.Ok() : ResultadoCompatibilidade.Incompativel(["fake"]));
    }
}
