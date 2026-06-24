using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Legislativo.Application.Lexml;
using Tensorroot.Gov.Modules.Legislativo.Application.LimiteCamara;
using Tensorroot.Gov.Modules.Legislativo.Application.Normas;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure;

/// <summary>
/// Endpoints HTTP do acervo normativo e da prestacao do art. 29-A (W9.5): cadastro/busca de normas,
/// export LexML-BR (URN + XML) e o demonstrativo do limite de despesa da Camara.
/// </summary>
internal static partial class LegislativoEndpoints
{
    private static void MapearNormas(RouteGroupBuilder grupo)
    {
        grupo.MapGet("/normas", async (
            string? termo, int? tipo, int? numero, int? ano, int? situacao, int? pagina, int? tamanho,
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new BuscarNormasQuery(termo, tipo, numero, ano, situacao, pagina ?? 1, tamanho ?? 20), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapGet("/normas/{normaId:guid}", async (
            Guid normaId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterNormaPorIdQuery(normaId), cancellationToken)))
            .RequirePermission("legislativo.ver");

        // Export LexML-BR (URN + XML) da norma — dados abertos/prestacao LOCAL (W9.5). Transmissao
        // oficial a base LexML = M10. Devolve o XML como arquivo (download) com a URN no header.
        grupo.MapGet("/normas/{normaId:guid}/lexml", async (
            Guid normaId, ISender sender, CancellationToken cancellationToken) =>
        {
            var lexml = await sender.Send(new ExportarNormaLexmlQuery(normaId), cancellationToken);
            if (lexml is null)
            {
                return Results.NotFound();
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(lexml.Xml);
            return Results.File(bytes, "application/xml", lexml.NomeArquivo);
        }).RequirePermission("legislativo.ver");

        grupo.MapPost("/normas", async (
            CadastrarNormaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("legislativo.normas.gerenciar");

        grupo.MapPost("/normas/{normaId:guid}/revogacao", async (
            Guid normaId, RevogarNormaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RevogarNormaCommand(normaId, payload.DataRevogacao, payload.NormaRevogadoraId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.normas.gerenciar");

        grupo.MapPost("/normas/{normaId:guid}/alteracao", async (
            Guid normaId, RegistrarAlteracaoNormaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarAlteracaoNormaCommand(normaId, payload.DataReferencia, payload.NormaAlteradoraId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.normas.gerenciar");

        grupo.MapPost("/normas/{normaId:guid}/proposicao-origem", async (
            Guid normaId, ProposicaoOrigemPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new VincularProposicaoOrigemCommand(normaId, payload.ProposicaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("legislativo.normas.gerenciar");
    }

    private static void MapearLimiteCamara(RouteGroupBuilder grupo)
    {
        // Prestacao do art. 29-A (limite de despesa da Camara). Transmissao ao TCE-RS = M10 (gera/valida local).
        grupo.MapPost("/limite-camara/apuracoes", async (
            AbrirApuracaoArt29ACommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("legislativo.limite-camara.gerenciar");

        grupo.MapGet("/limite-camara/apuracoes/{exercicio:int}", async (
            int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterDemonstrativoArt29AQuery(exercicio), cancellationToken)))
            .RequirePermission("legislativo.ver");

        grupo.MapPost("/limite-camara/apuracoes/{apuracaoId:guid}/consolidacao", async (
            Guid apuracaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ConsolidarApuracaoArt29ACommand(apuracaoId), cancellationToken)))
            .RequirePermission("legislativo.limite-camara.gerenciar");
    }

    private sealed record RevogarNormaPayload(DateOnly DataRevogacao, Guid? NormaRevogadoraId);

    private sealed record RegistrarAlteracaoNormaPayload(DateOnly DataReferencia, Guid NormaAlteradoraId);

    private sealed record ProposicaoOrigemPayload(Guid ProposicaoId);
}
