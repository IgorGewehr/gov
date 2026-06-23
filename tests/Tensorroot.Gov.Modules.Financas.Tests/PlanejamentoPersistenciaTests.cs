using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Integracoes;
using Tensorroot.Gov.Modules.Financas.Application.Planejamento.Compatibilidade;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Tensorroot.Gov.Modules.Financas.Tests;

/// <summary>
/// Persistência do planejamento (SQLite em memória): valida os mapeamentos EF da árvore PPA,
/// a cadeia de compatibilidade LOA ⊆ LDO ⊆ PPA pelo serviço real, e o VÍNCULO "a dotação nasce
/// da LOA" pelo handler de integração real (idempotente).
/// </summary>
public sealed class PlanejamentoPersistenciaTests
{
    private static readonly Guid Tenant = Guid.Parse("88888888-8888-8888-8888-888888888888");

    private static FinancasDbContext NovoContexto(SqliteConnection conexao)
    {
        var options = new DbContextOptionsBuilder<FinancasDbContext>().UseSqlite(conexao).Options;
        return new FinancasDbContext(options, new TenantFake());
    }

    [Fact]
    public async Task Arvore_ppa_persiste_e_recarrega_com_programas_acoes_metas()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        await using (var ctx = NovoContexto(conexao))
        {
            await ctx.Database.EnsureCreatedAsync();
            var (ppa, _) = MontarPpaVigente();
            ctx.Ppas.Add(ppa);
            await ctx.SaveChangesAsync();
        }

        await using var leitura = NovoContexto(conexao);
        var recarregado = await leitura.Ppas.FirstAsync();
        recarregado.Programas.Should().ContainSingle();
        recarregado.Programas.Single().Acoes.Should().ContainSingle();
        recarregado.Programas.Single().Acoes.Single().Metas.Should().ContainSingle();
    }

    [Fact]
    public async Task Compatibilidade_detecta_acao_nao_priorizada_na_ldo()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        await using var ctx = NovoContexto(conexao);
        await ctx.Database.EnsureCreatedAsync();

        var (ppa, acaoId) = MontarPpaVigente();
        ctx.Ppas.Add(ppa);

        // LDO vigente que NAO prioriza a acao da despesa fixada → incompatibilidade esperada.
        var ldo = LeiDiretrizes.Criar(Tenant, 2026, ppa, "LDO 1", 2025);
        ldo.DefinirMetaFiscal(2026, ValorMonetario.De(1000m), ValorMonetario.De(1000m), 0m, 0m, ValorMonetario.De(0m));
        ldo.ColocarEmTramitacao();
        ldo.Vigorar("LDO 1", 2025, exigirAmf: false, exigirArf: false);
        ctx.Ldos.Add(ldo);

        var loa = LeiOrcamentariaAnual.Criar(Tenant, 2026, ldo, 25m, "LOA 1", 2025);
        loa.PreverReceita(
            NaturezaReceita.De(CategoriaEconomicaReceita.ReceitasCorrentes, "1", "1.1", "1.1.1"),
            "0001", ValorMonetario.De(50000m));
        loa.FixarDespesa(Classificacao(), acaoId, "3.3.90.30", ValorMonetario.De(50000m));
        ctx.Loas.Add(loa);
        await ctx.SaveChangesAsync();

        var servico = new CompatibilidadeOrcamentariaService(new PpaRepository(ctx), new LdoRepository(ctx));
        var resultado = await servico.VerificarAsync(loa, CancellationToken.None);

        resultado.Compativel.Should().BeFalse();
        resultado.Motivos.Should().Contain(m => m.Contains("nao priorizada na LDO", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Compatibilidade_detecta_desequilibrio_receita_menor_que_despesa()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        await using var ctx = NovoContexto(conexao);
        await ctx.Database.EnsureCreatedAsync();

        var (loa, _) = MontarCadeiaCompletaDesequilibrada(ctx);
        await ctx.SaveChangesAsync();

        var servico = new CompatibilidadeOrcamentariaService(new PpaRepository(ctx), new LdoRepository(ctx));
        var resultado = await servico.VerificarAsync(loa, CancellationToken.None);

        resultado.Compativel.Should().BeFalse();
        resultado.Motivos.Should().Contain(m => m.Contains("Desequilibrio", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Dotacao_nasce_da_loa_pelo_handler_idempotente()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        await using var ctx = NovoContexto(conexao);
        await ctx.Database.EnsureCreatedAsync();

        var (loa, _) = MontarCadeiaCompletaEquilibrada(ctx);
        await ctx.SaveChangesAsync(); // persiste PPA/LDO/LOA antes da checagem de compatibilidade
        var servicoCompat = new CompatibilidadeOrcamentariaService(new PpaRepository(ctx), new LdoRepository(ctx));
        await loa.AprovarAsync(servicoCompat, CancellationToken.None);
        loa.EntrarEmExecucao();
        var item = loa.Itens.Single();
        await ctx.SaveChangesAsync();

        var handler = new GerarDotacaoDeItemLoaHandler(new LoaRepository(ctx), new DotacaoOrcamentariaRepository(ctx), ctx, new TenantFake());
        var evento = new ItemLoaEntrouEmExecucao(loa.Id, item.Id, loa.Exercicio, item.Classificacao, item.ValorFixado, item.AcaoPpaId);

        await handler.Handle(evento, CancellationToken.None);
        await handler.Handle(evento, CancellationToken.None); // idempotente

        var dotacoes = await ctx.Dotacoes.ToListAsync();
        dotacoes.Should().ContainSingle();
        dotacoes.Single().ValorDotadoInicial.Valor.Should().Be(item.ValorFixado.Valor);
        dotacoes.Single().LoaId.Should().Be(loa.Id.Value);
        dotacoes.Single().ItemDespesaFixadaId.Should().Be(item.Id.Value);

        var loaRecarregada = await ctx.Loas.FirstAsync();
        loaRecarregada.Itens.Single().DotacaoGerada.Should().BeTrue();
    }

    private static ClassificacaoOrcamentaria Classificacao()
        => ClassificacaoOrcamentaria.De("02", "0201", "04.122.0002.2010", CategoriaEconomica.DespesasCorrentes, "0001");

    private static (PlanoPlurianual Ppa, AcaoPpaId AcaoId) MontarPpaVigente()
    {
        var ppa = PlanoPlurianual.Criar(Tenant, 2026, "PPA 1", 2025);
        var programaId = ppa.AdicionarPrograma("0002", "Educacao", "Obj", "Alunos", "IDEB", 4m, 5m);
        var acaoId = ppa.AdicionarAcao(programaId, "2010", "Manutencao", TipoAcao.Atividade, "12.361.0002.2010", "Aluno", "unidade");
        ppa.DefinirMeta(acaoId, 2026, 100m, "unidade", ValorMonetario.De(50000m), "Sede");
        ppa.ColocarEmTramitacao();
        ppa.Vigorar("PPA 1", 2025);
        return (ppa, acaoId);
    }

    private static LeiDiretrizes LdoVigentePriorizando(PlanoPlurianual ppa, AcaoPpaId acaoId)
    {
        var ldo = LeiDiretrizes.Criar(Tenant, 2026, ppa, "LDO 1", 2025);
        ldo.PriorizarAcao(ppa, acaoId, 1, "prioridade");
        ldo.DefinirMetaFiscal(2026, ValorMonetario.De(100000m), ValorMonetario.De(100000m), 0m, 0m, ValorMonetario.De(0m));
        ldo.ColocarEmTramitacao();
        ldo.Vigorar("LDO 1", 2025, exigirAmf: false, exigirArf: false);
        return ldo;
    }

    private static (LeiOrcamentariaAnual Loa, AcaoPpaId AcaoId) MontarCadeiaCompletaEquilibrada(FinancasDbContext ctx)
    {
        var (ppa, acaoId) = MontarPpaVigente();
        var ldo = LdoVigentePriorizando(ppa, acaoId);
        ctx.Ppas.Add(ppa);
        ctx.Ldos.Add(ldo);

        var loa = LeiOrcamentariaAnual.Criar(Tenant, 2026, ldo, 25m, "LOA 1", 2025);
        loa.PreverReceita(NaturezaReceita.De(CategoriaEconomicaReceita.ReceitasCorrentes, "1", "1.1", "1.1.1"), "0001", ValorMonetario.De(50000m));
        loa.FixarDespesa(Classificacao(), acaoId, "3.3.90.30", ValorMonetario.De(50000m));
        ctx.Loas.Add(loa);
        return (loa, acaoId);
    }

    private static (LeiOrcamentariaAnual Loa, AcaoPpaId AcaoId) MontarCadeiaCompletaDesequilibrada(FinancasDbContext ctx)
    {
        var (ppa, acaoId) = MontarPpaVigente();
        var ldo = LdoVigentePriorizando(ppa, acaoId);
        ctx.Ppas.Add(ppa);
        ctx.Ldos.Add(ldo);

        var loa = LeiOrcamentariaAnual.Criar(Tenant, 2026, ldo, 25m, "LOA 1", 2025);
        loa.PreverReceita(NaturezaReceita.De(CategoriaEconomicaReceita.ReceitasCorrentes, "1", "1.1", "1.1.1"), "0001", ValorMonetario.De(10000m));
        loa.FixarDespesa(Classificacao(), acaoId, "3.3.90.30", ValorMonetario.De(50000m)); // despesa > receita
        ctx.Loas.Add(loa);
        return (loa, acaoId);
    }

    private sealed class TenantFake : ITenantContext
    {
        public Guid TenantId => Tenant;

        public bool HasTenant => true;
    }
}
