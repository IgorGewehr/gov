using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;
using Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Repositories;
using Xunit;
using VereadorIdVoto = Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes.VereadorId;

namespace Tensorroot.Gov.Modules.Legislativo.Tests;

/// <summary>
/// Cobertura do handler <see cref="AprovarProposicaoHandler"/> — o ponto da orquestracao mais
/// critico da demo (BUG-1): so aprova a materia se a votacao for DAQUELA proposicao, estiver
/// encerrada e tiver aprovado; e mapeia a maioria EFETIVAMENTE ATINGIDA, nao a meramente exigida.
/// </summary>
public sealed class AprovarProposicaoHandlerTests : LegislativoTestBase
{
    private static readonly DateTimeOffset Quando = new(2026, 6, 22, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoje = new(2026, 6, 22);

    private static Proposicao ProposicaoEmOrdemDoDia(LegislativoDbContext ctx, TipoProposicao tipo = TipoProposicao.ProjetoDeLeiOrdinaria)
    {
        var proposicao = Proposicao.Apresentar(
            TenantA,
            tipo,
            Ementa.De("Dispoe sobre o servico publico municipal"),
            Autoria.De("Vereador Joao"),
            RegimeTramitacao.Ordinario,
            Hoje,
            Guid.NewGuid().ToString("N")[..10]);
        proposicao.Distribuir(Hoje);
        proposicao.RegistrarParecer(Proposicao.ComissaoCcj, favoravel: true, Hoje);
        proposicao.RegistrarParecer(Proposicao.ComissaoFinancasOrcamento, favoravel: true, Hoje);
        proposicao.IncluirEmOrdemDoDia(Hoje);
        ctx.Proposicoes.Add(proposicao);
        return proposicao;
    }

    private static Votacao VotacaoAprovada(
        LegislativoDbContext ctx,
        ProposicaoId proposicaoId,
        MaioriaExigida maioria = MaioriaExigida.Simples,
        int totalMembros = 11,
        int presentes = 9,
        int simVotos = 6)
    {
        var votacao = Votacao.Iniciar(TenantA, SessaoId.New(), proposicaoId, TipoVotacao.Nominal, maioria, totalMembros, presentes, 1);
        for (var i = 0; i < simVotos; i++)
        {
            votacao.RegistrarVoto(VotoId.New(), VereadorIdVoto.New(), SentidoVoto.Sim, Quando);
        }

        votacao.Encerrar();
        ctx.Votacoes.Add(votacao);
        return votacao;
    }

    private static AprovarProposicaoHandler Handler(LegislativoDbContext ctx)
        => new(new ProposicaoRepository(ctx), new VotacaoRepository(ctx), ctx, ParametrosFixos, new FixedTimeProvider(Quando));

    // Intersticio de 1 dia (parametrizavel por tenant): suficiente para as materias de turno unico
    // destes testes; o rito de 2 turnos da Emenda a LOM e coberto em ProposicaoFluxoTests.
    private static readonly ILegislativoParametros ParametrosFixos = new ParametrosTeste(Interstico.DeDias(1));

    private sealed class ParametrosTeste(Interstico intersticio) : ILegislativoParametros
    {
        public Interstico IntersticioEntreTurnos() => intersticio;
    }

    [Fact] // Item 9 (o teste mais importante): aprovar com votacao de OUTRA proposicao falha (BUG-1).
    public async Task Aprovar_com_votacao_de_outra_proposicao_falha()
    {
        await using var ctx = CriarContexto(TenantA);
        var materiaX = ProposicaoEmOrdemDoDia(ctx);
        var materiaY = ProposicaoEmOrdemDoDia(ctx);
        var votacaoDeY = VotacaoAprovada(ctx, materiaY.Id); // votacao pertence a Y
        await ctx.SaveChangesAsync();

        // Tentar aprovar X com o resultado da votacao de Y deve ser recusado.
        var acao = async () => await Handler(ctx).Handle(
            new AprovarProposicaoCommand(materiaX.Id.Value, votacaoDeY.Id.Value), default);

        await acao.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*nao pertence a esta proposicao*");

        var persistida = await ctx.Proposicoes.AsNoTracking().SingleAsync(p => p.Id == materiaX.Id);
        persistida.Situacao.Should().Be(SituacaoProposicao.EmOrdemDoDia); // intacta
    }

    [Fact] // Caminho feliz: votacao da propria proposicao, encerrada e aprovada -> aprova a materia.
    public async Task Aprovar_com_votacao_da_propria_proposicao_aprova()
    {
        await using var ctx = CriarContexto(TenantA);
        var materia = ProposicaoEmOrdemDoDia(ctx);
        var votacao = VotacaoAprovada(ctx, materia.Id);
        await ctx.SaveChangesAsync();

        await Handler(ctx).Handle(new AprovarProposicaoCommand(materia.Id.Value, votacao.Id.Value), default);

        var persistida = await ctx.Proposicoes.AsNoTracking().SingleAsync(p => p.Id == materia.Id);
        persistida.Situacao.Should().Be(SituacaoProposicao.Aprovada);
    }

    [Fact] // Item 10: PLC so aprova com maioria ATINGIDA >= Absoluta; placar de maioria simples falha.
    public async Task Aprovar_PLC_com_placar_apenas_simples_falha_o_mapeamento()
    {
        await using var ctx = CriarContexto(TenantA);
        var plc = ProposicaoEmOrdemDoDia(ctx, TipoProposicao.ProjetoDeLeiComplementar); // exige Absoluta
        // Votacao aprovada pela maioria exigida Simples, mas placar (6 de 11) atinge Absoluta -> aprova.
        var atingeAbsoluta = VotacaoAprovada(ctx, plc.Id, MaioriaExigida.Simples, totalMembros: 11, presentes: 11, simVotos: 6);
        await ctx.SaveChangesAsync();

        await Handler(ctx).Handle(new AprovarProposicaoCommand(plc.Id.Value, atingeAbsoluta.Id.Value), default);
        (await ctx.Proposicoes.AsNoTracking().SingleAsync(p => p.Id == plc.Id))
            .Situacao.Should().Be(SituacaoProposicao.Aprovada);

        // Outro PLC cuja votacao aprovou por Simples mas o placar NAO atinge a Absoluta -> Aprovar falha.
        var plc2 = ProposicaoEmOrdemDoDia(ctx, TipoProposicao.ProjetoDeLeiComplementar);
        var soSimples = VotacaoAprovada(ctx, plc2.Id, MaioriaExigida.Simples, totalMembros: 11, presentes: 9, simVotos: 5);
        await ctx.SaveChangesAsync();

        var acao = async () => await Handler(ctx).Handle(
            new AprovarProposicaoCommand(plc2.Id.Value, soSimples.Id.Value), default);
        await acao.Should().ThrowAsync<InvalidOperationException>(); // maioria exigida (Absoluta) nao atingida
    }

    [Fact] // Votacao ainda Aberta (nao encerrada) nao pode aprovar a materia.
    public async Task Aprovar_com_votacao_aberta_falha()
    {
        await using var ctx = CriarContexto(TenantA);
        var materia = ProposicaoEmOrdemDoDia(ctx);
        var aberta = Votacao.Iniciar(TenantA, SessaoId.New(), materia.Id, TipoVotacao.Nominal, MaioriaExigida.Simples, 11, 9, 1);
        aberta.RegistrarVoto(VotoId.New(), VereadorIdVoto.New(), SentidoVoto.Sim, Quando);
        ctx.Votacoes.Add(aberta);
        await ctx.SaveChangesAsync();

        var acao = async () => await Handler(ctx).Handle(
            new AprovarProposicaoCommand(materia.Id.Value, aberta.Id.Value), default);

        await acao.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Encerrada*");
    }
}
