using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Consignacoes;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;

/// <summary>
/// Endpoints HTTP das CONSIGNACOES + MARGEM CONSIGNAVEL (Onda 2 — Lei 14.131/2021): cadastro de
/// consignatarias, rubricas consignaveis, consulta de margem (3 baldes), averbacao/suspensao/cancelamento
/// e o lancamento dos descontos consignados na folha respeitando a margem (corte por prioridade).
/// </summary>
internal static partial class RecursosHumanosEndpoints
{
    private static void MapearConsignacoes(RouteGroupBuilder grupo)
    {
        MapearConsignatarias(grupo);
        MapearRubricasConsignaveis(grupo);
        MapearContratosConsignacao(grupo);
    }

    private static void MapearConsignatarias(RouteGroupBuilder grupo)
    {
        // Cadastro mestre de consignatarias (banco/entidade habilitada).
        grupo.MapPost("/consignatarias", async (
            CadastrarConsignatariaCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("rh.consignacao.gerenciar");

        grupo.MapGet("/consignatarias", async (ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListarConsignatariasQuery(), ct)))
            .RequirePermission("rh.consignacao.ver");

        grupo.MapPost("/consignatarias/{consignatariaId:guid}/suspensao", async (
            Guid consignatariaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new SuspenderConsignatariaCommand(consignatariaId), ct);
            return Results.NoContent();
        })
            .RequirePermission("rh.consignacao.gerenciar");

        grupo.MapPost("/consignatarias/{consignatariaId:guid}/reativacao", async (
            Guid consignatariaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ReativarConsignatariaCommand(consignatariaId), ct);
            return Results.NoContent();
        })
            .RequirePermission("rh.consignacao.gerenciar");
    }

    private static void MapearRubricasConsignaveis(RouteGroupBuilder grupo)
    {
        // Parametrizacao das rubricas consignaveis (categoria/prioridade, balde de margem, base da margem).
        grupo.MapPost("/rubricas-consignaveis", async (
            DefinirRubricaConsignavelCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("rh.consignacao.gerenciar");
    }

    private static void MapearContratosConsignacao(RouteGroupBuilder grupo)
    {
        // MARGEM do servidor numa competencia: 3 baldes (limite/comprometido/disponivel) — base apurada da folha.
        grupo.MapGet("/servidores/{servidorId:guid}/margem", async (
            Guid servidorId, int ano, int mes, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ConsultarMargemQuery(servidorId, ano, mes), ct)))
            .RequirePermission("rh.consignacao.ver");

        // Consignacoes (historico) do servidor.
        grupo.MapGet("/servidores/{servidorId:guid}/consignacoes", async (
            Guid servidorId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListarConsignacoesDoServidorQuery(servidorId), ct)))
            .RequirePermission("rh.consignacao.ver");

        // AVERBAR: so aceita se a parcela couber na margem disponivel do balde (invariante do agregado).
        grupo.MapPost("/consignacoes", async (
            AverbarConsignacaoCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("rh.consignacao.averbar");

        grupo.MapPost("/consignacoes/{contratoId:guid}/suspensao", async (
            Guid contratoId, SuspenderConsignacaoPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new SuspenderConsignacaoCommand(contratoId, payload.Motivo), ct);
            return Results.NoContent();
        })
            .RequirePermission("rh.consignacao.gerenciar");

        grupo.MapPost("/consignacoes/{contratoId:guid}/reativacao", async (
            Guid contratoId, ReativarConsignacaoPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ReativarConsignacaoCommand(contratoId, payload.DataReferencia), ct);
            return Results.NoContent();
        })
            .RequirePermission("rh.consignacao.gerenciar");

        grupo.MapPost("/consignacoes/{contratoId:guid}/cancelamento", async (
            Guid contratoId, CancelarConsignacaoPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new CancelarConsignacaoCommand(contratoId, payload.Motivo), ct);
            return Results.NoContent();
        })
            .RequirePermission("rh.consignacao.gerenciar");

        // EFEITO NA FOLHA: lanca os descontos consignados na folha aberta respeitando a margem (corte por
        // prioridade). Rodar APOS a consolidacao dos descontos legais e ANTES do fechamento (design RH §2.5).
        grupo.MapPost("/folhas/{folhaId:guid}/consignados", async (
            Guid folhaId, ISender sender, CancellationToken ct)
            => Results.Ok(new { lancados = await sender.Send(new LancarConsignadosNaFolhaCommand(folhaId), ct) }))
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private sealed record SuspenderConsignacaoPayload(string Motivo);

    private sealed record ReativarConsignacaoPayload(DateOnly DataReferencia);

    private sealed record CancelarConsignacaoPayload(string Motivo);
}
