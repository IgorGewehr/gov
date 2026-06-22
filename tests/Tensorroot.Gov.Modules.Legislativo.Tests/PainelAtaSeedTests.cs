using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Legislativo.Application.Atas;
using Tensorroot.Gov.Modules.Legislativo.Application.Demonstracao;
using Tensorroot.Gov.Modules.Legislativo.Application.Votacoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Vereadores;
using Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// Cobertura dos demo-enablers a nivel de aplicacao: o seed idempotente, o painel ao vivo (placar
/// agregado COM o nome do vereador) e a ata estruturada (reflete presenca, pauta e resultados).
/// Usa os repositorios reais sobre o <see cref="LegislativoDbContext"/> (IUnitOfWork) em SQLite.
/// </summary>
public sealed class PainelAtaSeedTests : LegislativoTestBase
{
    private static readonly DateTimeOffset Quando = new(2026, 6, 22, 14, 0, 0, TimeSpan.Zero);

    private static SemearDemonstracaoLegislativaHandler Seeder(LegislativoDbContext ctx)
        => new(
            new VereadorRepository(ctx),
            new ProposicaoRepository(ctx),
            new SessaoRepository(ctx),
            new VotacaoRepository(ctx),
            ctx,
            new TenantContextFake(TenantA),
            new FixedTimeProvider(Quando));

    [Fact] // Seed cria o cenario completo (9 vereadores + proposicao + sessao + votacao nominal).
    public async Task Seed_cria_cenario_de_demonstracao()
    {
        await using var ctx = CriarContexto(TenantA);

        var resultado = await Seeder(ctx).Handle(new SemearDemonstracaoLegislativaCommand(), default);

        resultado.JaSemeado.Should().BeFalse();
        resultado.Vereadores.Should().Be(9);
        resultado.ProposicaoId.Should().NotBeEmpty();
        resultado.SessaoId.Should().NotBeEmpty();
        resultado.VotacaoId.Should().NotBeEmpty();

        (await ctx.Vereadores.CountAsync()).Should().Be(9);
        (await ctx.Sessoes.CountAsync()).Should().Be(1);
        (await ctx.Votacoes.Include(v => v.Votos).SingleAsync()).Votos.Should().NotBeEmpty();
    }

    [Fact] // Idempotencia: rodar o seed duas vezes nao duplica nada.
    public async Task Seed_e_idempotente()
    {
        await using var ctx = CriarContexto(TenantA);
        await Seeder(ctx).Handle(new SemearDemonstracaoLegislativaCommand(), default);

        var segundo = await Seeder(ctx).Handle(new SemearDemonstracaoLegislativaCommand(), default);

        segundo.JaSemeado.Should().BeTrue();
        (await ctx.Vereadores.CountAsync()).Should().Be(9);
    }

    [Fact] // Painel: agrega o placar e resolve o NOME do vereador (nao GUID cru).
    public async Task Painel_agrega_placar_com_nomes()
    {
        await using var ctx = CriarContexto(TenantA);
        var seed = await Seeder(ctx).Handle(new SemearDemonstracaoLegislativaCommand(), default);

        var painel = await new ObterPainelDaVotacaoHandler(new VotacaoRepository(ctx), new VereadorRepository(ctx))
            .Handle(new ObterPainelDaVotacaoQuery(seed.VotacaoId), default);

        // 8 presentes votam (Sim/Sim/Abstencao/Nao em ciclo de 4 -> 4 Sim, 2 Abst, 2 Nao).
        painel.TotalVotos.Should().Be(8);
        (painel.Sim + painel.Nao + painel.Abstencao).Should().Be(painel.TotalVotos);
        painel.Ausentes.Should().Be(painel.TotalMembros - painel.TotalVotos);
        painel.Votos.Should().HaveCount(8);
        painel.Votos.Should().OnlyContain(linha => !linha.NomeParlamentar.Contains('-'));
        painel.Votos.Select(linha => linha.NomeParlamentar).Should().Contain("Ana Paula");
    }

    [Fact] // Ata: reflete presencas (com ausente), pauta e o resultado/placar da votacao.
    public async Task Ata_reflete_a_sessao()
    {
        await using var ctx = CriarContexto(TenantA);
        var seed = await Seeder(ctx).Handle(new SemearDemonstracaoLegislativaCommand(), default);

        var ata = await new GerarAtaDaSessaoHandler(
                new SessaoRepository(ctx),
                new VotacaoRepository(ctx),
                new VereadorRepository(ctx),
                new ProposicaoRepository(ctx))
            .Handle(new GerarAtaDaSessaoQuery(seed.SessaoId), default);

        ata.TotalMembros.Should().Be(9);
        ata.Presentes.Should().Be(8);
        ata.Ausentes.Should().Be(1);
        ata.QuorumAtingido.Should().BeTrue();
        ata.OrdemDoDia.Should().ContainSingle().Which.Ementa.Should().Contain("logradouro");
        ata.Presencas.Should().Contain(p => p.NomeParlamentar == "Ana Paula" && p.Presente);
        var votacao = ata.Votacoes.Should().ContainSingle().Subject;
        (votacao.Sim + votacao.Nao + votacao.Abstencao).Should().Be(8);
        votacao.VotantesSim.Should().NotBeEmpty();
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset agora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => agora;
}
