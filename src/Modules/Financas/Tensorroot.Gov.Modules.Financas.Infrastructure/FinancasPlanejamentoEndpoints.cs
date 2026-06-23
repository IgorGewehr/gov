using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Financas.Application.Planejamento.Creditos;
using Tensorroot.Gov.Modules.Financas.Application.Planejamento.Ldo;
using Tensorroot.Gov.Modules.Financas.Application.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Application.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Application.Planejamento.Queries;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure;

/// <summary>
/// Endpoints HTTP do planejamento orçamentário (PPA/LDO/LOA + créditos adicionais).
/// Leituras → "financas.ver"; mutações de planejamento → "financas.planejar" (segregado da execução).
/// </summary>
internal static class FinancasPlanejamentoEndpoints
{
    public static void Map(RouteGroupBuilder grupo)
    {
        MapearPpa(grupo);
        MapearLdo(grupo);
        MapearLoa(grupo);
        MapearCreditos(grupo);
    }

    private static void MapearPpa(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/planejamento/ppa", async (CriarPpaCommand cmd, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(cmd, ct) })).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/ppa/programas", async (AdicionarProgramaCommand cmd, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(cmd, ct) })).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/ppa/acoes", async (AdicionarAcaoCommand cmd, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(cmd, ct) })).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/ppa/metas", async (DefinirMetaCommand cmd, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(cmd, ct);
            return Results.NoContent();
        }).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/ppa/{ppaId:guid}/tramitar", async (Guid ppaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new TramitarPpaCommand(ppaId), ct);
            return Results.NoContent();
        }).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/ppa/{ppaId:guid}/vigorar", async (Guid ppaId, VigorarPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new VigorarPpaCommand(ppaId, payload.NumeroLei, payload.AnoLei), ct);
            return Results.NoContent();
        }).RequirePermission("financas.planejar");

        grupo.MapGet("/planejamento/ppa/{ppaId:guid}", async (Guid ppaId, ISender sender, CancellationToken ct)
            => await sender.Send(new ConsultarPpaQuery(ppaId), ct) is { } r ? Results.Ok(r) : Results.NotFound())
            .RequirePermission("financas.ver");
    }

    private static void MapearLdo(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/planejamento/ldo", async (CriarLdoCommand cmd, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(cmd, ct) })).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/ldo/prioridades", async (PriorizarAcaoCommand cmd, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(cmd, ct);
            return Results.NoContent();
        }).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/ldo/metas-fiscais", async (DefinirMetaFiscalCommand cmd, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(cmd, ct);
            return Results.NoContent();
        }).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/ldo/anexos", async (AnexarLdoCommand cmd, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(cmd, ct);
            return Results.NoContent();
        }).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/ldo/{ldoId:guid}/tramitar", async (Guid ldoId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new TramitarLdoCommand(ldoId), ct);
            return Results.NoContent();
        }).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/ldo/{ldoId:guid}/vigorar", async (Guid ldoId, VigorarPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new VigorarLdoCommand(ldoId, payload.NumeroLei, payload.AnoLei), ct);
            return Results.NoContent();
        }).RequirePermission("financas.planejar");

        grupo.MapGet("/planejamento/ldo/{ldoId:guid}", async (Guid ldoId, ISender sender, CancellationToken ct)
            => await sender.Send(new ConsultarLdoQuery(ldoId), ct) is { } r ? Results.Ok(r) : Results.NotFound())
            .RequirePermission("financas.ver");
    }

    private static void MapearLoa(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/planejamento/loa", async (CriarLoaCommand cmd, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(cmd, ct) })).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/loa/receitas", async (PreverReceitaCommand cmd, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(cmd, ct) })).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/loa/despesas", async (FixarDespesaCommand cmd, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(cmd, ct) })).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/loa/{loaId:guid}/tramitar", async (Guid loaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new TramitarLoaCommand(loaId), ct);
            return Results.NoContent();
        }).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/loa/{loaId:guid}/aprovar", async (Guid loaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AprovarLoaCommand(loaId), ct);
            return Results.NoContent();
        }).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/loa/{loaId:guid}/executar", async (Guid loaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ColocarLoaEmExecucaoCommand(loaId), ct);
            return Results.NoContent();
        }).RequirePermission("financas.planejar");

        grupo.MapPost("/planejamento/loa/{loaId:guid}/encerrar", async (Guid loaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new EncerrarLoaCommand(loaId), ct);
            return Results.NoContent();
        }).RequirePermission("financas.planejar");

        grupo.MapGet("/planejamento/loa/{loaId:guid}", async (Guid loaId, ISender sender, CancellationToken ct)
            => await sender.Send(new ConsultarLoaQuery(loaId), ct) is { } r ? Results.Ok(r) : Results.NotFound())
            .RequirePermission("financas.ver");

        grupo.MapGet("/planejamento/loa/{loaId:guid}/qdd", async (Guid loaId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListarItensQddQuery(loaId), ct))).RequirePermission("financas.ver");

        grupo.MapGet("/planejamento/loa/{loaId:guid}/compatibilidade", async (Guid loaId, ISender sender, CancellationToken ct)
            => await sender.Send(new ConsultarCompatibilidadeQuery(loaId), ct) is { } r ? Results.Ok(r) : Results.NotFound())
            .RequirePermission("financas.ver");
    }

    private static void MapearCreditos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/planejamento/creditos-adicionais", async (AbrirCreditoAdicionalCommand cmd, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(cmd, ct) })).RequirePermission("financas.planejar");
    }

    private sealed record VigorarPayload(string NumeroLei, int AnoLei);
}
