using FluentAssertions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Tensorroot.Gov.Modules.Administracao.Tests;

/// <summary>
/// Cobertura de borda do bug de listagem de licitacoes (GET /api/administracao/licitacoes):
/// sem o filtro de situacao a consulta deve retornar TODAS as licitacoes do tenant, sem estourar.
/// O <see cref="LicitacaoRepository"/> passa a aceitar situacao nula (sem filtro). Tenant-scoped pelo
/// Global Query Filter. Exercita o repositorio real sobre SQLite.
/// </summary>
public sealed class LicitacaoListagemBugTests : AdministracaoTestBase
{
    private static Licitacao NovoPregao(string objeto)
        => Licitacao.Abrir(
            TenantA,
            objeto,
            ModalidadeLicitacao.Pregao,
            CriterioJulgamento.MenorPreco,
            ValorMonetario.De(200000m));

    private async Task SemearDuasSituacoesAsync()
    {
        await using var contexto = CriarContexto(TenantA);

        // Uma Aberta (estado inicial) e uma levada a EmJulgamento, para distinguir "todas" de "filtradas".
        contexto.Licitacoes.Add(NovoPregao("Aquisicao de notebooks"));

        var emJulgamento = NovoPregao("Aquisicao de mobiliario");
        var lote = emJulgamento.AdicionarLote(1, "Lote 1", ValorMonetario.De(100000m));
        var proposta = emJulgamento.RegistrarProposta(Guid.NewGuid(), lote, ValorMonetario.De(80000m));
        emJulgamento.JulgarPropostas(proposta.Value);
        contexto.Licitacoes.Add(emJulgamento);

        await contexto.SaveChangesAsync();
    }

    [Fact] // Bug: sem situacao (null) a listagem retorna TODAS as licitacoes do tenant, sem estourar.
    public async Task Listar_sem_filtro_de_situacao_retorna_todas()
    {
        await SemearDuasSituacoesAsync();

        await using var contexto = CriarContexto(TenantA);
        var repositorio = new LicitacaoRepository(contexto);

        var todas = await repositorio.ListarPorSituacaoAsync(null, CancellationToken.None);

        todas.Should().HaveCount(2);
        todas.Select(l => l.Situacao).Should().Contain(new[] { SituacaoLicitacao.Aberta, SituacaoLicitacao.EmJulgamento });
    }

    [Fact] // Com situacao informada o filtro continua valendo (regressao do comportamento existente).
    public async Task Listar_com_situacao_filtra_apenas_a_situacao_informada()
    {
        await SemearDuasSituacoesAsync();

        await using var contexto = CriarContexto(TenantA);
        var repositorio = new LicitacaoRepository(contexto);

        var abertas = await repositorio.ListarPorSituacaoAsync(SituacaoLicitacao.Aberta, CancellationToken.None);

        abertas.Should().ContainSingle();
        abertas.Single().Situacao.Should().Be(SituacaoLicitacao.Aberta);
    }

    [Fact] // A listagem sem filtro continua tenant-scoped (Global Query Filter): outro tenant nao ve nada.
    public async Task Listar_sem_filtro_e_isolado_por_tenant()
    {
        await SemearDuasSituacoesAsync();

        await using var contexto = CriarContexto(TenantB);
        var repositorio = new LicitacaoRepository(contexto);

        var todas = await repositorio.ListarPorSituacaoAsync(null, CancellationToken.None);

        todas.Should().BeEmpty();
    }
}
