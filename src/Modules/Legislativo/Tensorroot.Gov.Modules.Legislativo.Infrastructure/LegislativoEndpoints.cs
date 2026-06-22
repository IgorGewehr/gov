using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Legislativo.Application.Atas;
using Tensorroot.Gov.Modules.Legislativo.Application.Demonstracao;
using Tensorroot.Gov.Modules.Legislativo.Application.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Application.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Application.Vereadores;
using Tensorroot.Gov.Modules.Legislativo.Application.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do modulo Legislativo.</summary>
internal static class LegislativoEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/legislativo").WithTags("Legislativo");

        MapearProposicoes(grupo);
        MapearSessoes(grupo);
        MapearVotacoes(grupo);
        MapearVereadores(grupo);
        MapearDemonstracao(grupo);
    }

    private static void MapearVereadores(RouteGroupBuilder grupo)
    {
        grupo.MapGet("/vereadores", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarVereadoresQuery(), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapGet("/vereadores/{vereadorId:guid}", async (
            Guid vereadorId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterVereadorPorIdQuery(vereadorId), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapPost("/vereadores", async (
            CadastrarVereadorCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("legislativo.vereadores.gerenciar");

        grupo.MapPut("/vereadores/{vereadorId:guid}", async (
            Guid vereadorId, EditarVereadorPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new EditarVereadorCommand(
                    vereadorId,
                    payload.NomeCivil,
                    payload.NomeParlamentar,
                    payload.Partido,
                    payload.CargoMesa,
                    payload.Situacao),
                cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.vereadores.gerenciar");
    }

    private static void MapearDemonstracao(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/demonstracao/seed", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new SemearDemonstracaoLegislativaCommand(), cancellationToken)))
            .RequirePermission("legislativo.demo.semear");
    }

    private static void MapearProposicoes(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/proposicoes", async (
            ApresentarProposicaoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("legislativo.gerenciar");

        grupo.MapGet("/proposicoes/{proposicaoId:guid}", async (
            Guid proposicaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterProposicaoPorIdQuery(proposicaoId), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapGet("/proposicoes", async (
            int situacao, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarProposicoesPorSituacaoQuery(situacao), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapGet("/proposicoes/{proposicaoId:guid}/tramitacao", async (
            Guid proposicaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterTramitacaoDaProposicaoQuery(proposicaoId), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapPost("/proposicoes/{proposicaoId:guid}/distribuicao", async (
            Guid proposicaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DistribuirProposicaoCommand(proposicaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/proposicoes/{proposicaoId:guid}/emendas", async (
            Guid proposicaoId, ApresentarEmendaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(new ApresentarEmendaCommand(proposicaoId, payload.Texto, payload.Autoria), cancellationToken) }))
            .RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/proposicoes/{proposicaoId:guid}/pareceres", async (
            Guid proposicaoId, RegistrarParecerPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarParecerCommand(proposicaoId, payload.Comissao, payload.Favoravel), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/proposicoes/{proposicaoId:guid}/ordem-do-dia", async (
            Guid proposicaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new IncluirEmOrdemDoDiaCommand(proposicaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/proposicoes/{proposicaoId:guid}/aprovacao", async (
            Guid proposicaoId, DeliberacaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AprovarProposicaoCommand(proposicaoId, payload.VotacaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/proposicoes/{proposicaoId:guid}/rejeicao", async (
            Guid proposicaoId, DeliberacaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RejeitarProposicaoCommand(proposicaoId, payload.VotacaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/proposicoes/{proposicaoId:guid}/autografo", async (
            Guid proposicaoId, GerarAutografoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new GerarAutografoCommand(proposicaoId, payload.NumeroAutografo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/proposicoes/{proposicaoId:guid}/arquivamento", async (
            Guid proposicaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ArquivarProposicaoCommand(proposicaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");
    }

    private static void MapearSessoes(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/sessoes", async (
            AgendarSessaoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("legislativo.gerenciar");

        grupo.MapGet("/sessoes/{sessaoId:guid}", async (
            Guid sessaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterSessaoPorIdQuery(sessaoId), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapGet("/sessoes/agendadas", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarSessoesAgendadasQuery(), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapGet("/sessoes/{sessaoId:guid}/presencas", async (
            Guid sessaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterPresencasDaSessaoQuery(sessaoId), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapGet("/sessoes/{sessaoId:guid}/ata", async (
            Guid sessaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new GerarAtaDaSessaoQuery(sessaoId), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapPost("/sessoes/{sessaoId:guid}/presencas", async (
            Guid sessaoId, RegistrarPresencaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarPresencaCommand(sessaoId, payload.VereadorId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/sessoes/{sessaoId:guid}/ordem-do-dia", async (
            Guid sessaoId, IncluirNaOrdemDoDiaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new IncluirNaOrdemDoDiaCommand(sessaoId, payload.ProposicaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/sessoes/{sessaoId:guid}/quorum", async (
            Guid sessaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { quorumAtingido = await sender.Send(new VerificarQuorumCommand(sessaoId), cancellationToken) }))
            .RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/sessoes/{sessaoId:guid}/abertura", async (
            Guid sessaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AbrirSessaoCommand(sessaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/sessoes/{sessaoId:guid}/suspensao", async (
            Guid sessaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new SuspenderSessaoCommand(sessaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/sessoes/{sessaoId:guid}/reabertura", async (
            Guid sessaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ReabrirSessaoCommand(sessaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/sessoes/{sessaoId:guid}/encerramento", async (
            Guid sessaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EncerrarSessaoCommand(sessaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/sessoes/{sessaoId:guid}/cancelamento", async (
            Guid sessaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarSessaoCommand(sessaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");
    }

    private static void MapearVotacoes(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/votacoes", async (
            IniciarVotacaoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("legislativo.gerenciar");

        grupo.MapGet("/votacoes/{votacaoId:guid}", async (
            Guid votacaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterVotacaoPorIdQuery(votacaoId), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapGet("/votacoes", async (
            Guid? sessaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarVotacoesQuery(sessaoId), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapGet("/votacoes/{votacaoId:guid}/placar", async (
            Guid votacaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterPlacarDaVotacaoQuery(votacaoId), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapGet("/votacoes/{votacaoId:guid}/painel", async (
            Guid votacaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterPainelDaVotacaoQuery(votacaoId), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapGet("/votacoes/{votacaoId:guid}/votos-nominais", async (
            Guid votacaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarVotosNominaisQuery(votacaoId), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapPost("/votacoes/{votacaoId:guid}/votos", async (
            Guid votacaoId, RegistrarVotoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarVotoCommand(votacaoId, payload.VotoId, payload.VereadorId, payload.Sentido), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/votacoes/{votacaoId:guid}/encerramento", async (
            Guid votacaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { resultado = await sender.Send(new EncerrarVotacaoCommand(votacaoId), cancellationToken) }))
            .RequirePermission("legislativo.gerenciar");

        grupo.MapPost("/votacoes/{votacaoId:guid}/cancelamento", async (
            Guid votacaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarVotacaoCommand(votacaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.gerenciar");
    }

    private sealed record ApresentarEmendaPayload(string Texto, string Autoria);

    private sealed record RegistrarParecerPayload(string Comissao, bool Favoravel);

    private sealed record DeliberacaoPayload(Guid VotacaoId);

    private sealed record GerarAutografoPayload(string NumeroAutografo);

    private sealed record RegistrarPresencaPayload(Guid VereadorId);

    private sealed record IncluirNaOrdemDoDiaPayload(Guid ProposicaoId);

    private sealed record RegistrarVotoPayload(Guid VotoId, Guid VereadorId, int Sentido);

    private sealed record EditarVereadorPayload(
        string NomeCivil,
        string NomeParlamentar,
        string Partido,
        int CargoMesa,
        int Situacao);
}
