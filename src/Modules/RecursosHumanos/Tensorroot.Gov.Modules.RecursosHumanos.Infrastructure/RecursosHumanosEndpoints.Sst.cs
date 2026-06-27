using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Sst;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;

/// <summary>Endpoints HTTP da SST / Saude Ocupacional (PPP, PCMSO, S-2210/S-2220/S-2240).</summary>
internal static partial class RecursosHumanosEndpoints
{
    private static void MapearSst(RouteGroupBuilder grupo)
    {
        // SAUDE E SEGURANCA DO TRABALHO (SST): monitoramento da saude (ASO), condicoes ambientais (agentes
        // nocivos), CAT, e os documentos consolidados PPP/PCMSO. Cada registro vira, sob demanda, um evento
        // eSocial nao-periodico (S-2220/S-2240/S-2210), idempotente por id de negocio.
        var sst = grupo.MapGroup("/sst").WithTags("RecursosHumanos.Sst");

        MapearSstExames(sst);
        MapearSstExposicoes(sst);
        MapearSstCat(sst);
        MapearSstDocumentos(sst);
    }

    private static void MapearSstExames(RouteGroupBuilder sst)
    {
        // ASO (S-2220 / PCMSO): registra o exame ocupacional do servidor.
        sst.MapPost("/exames-ocupacionais", async (
            RegistrarExameOcupacionalCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Ficha de saude ocupacional (monitoracao biologica) do servidor.
        sst.MapGet("/servidores/{servidorId:guid}/exames-ocupacionais", async (
            Guid servidorId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListarExamesOcupacionaisQuery(servidorId), ct)))
            .RequirePermission("recursoshumanos.ver");

        // Agenda do PCMSO (NR-07): ASO com proximo exame vencido/a vencer ate a data de corte.
        sst.MapGet("/pcmso/agenda", async (
            DateOnly ate, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ObterAgendaPcmsoQuery(ate), ct)))
            .RequirePermission("recursoshumanos.ver");

        // Gera o evento eSocial S-2220 (Monitoramento da Saude) a partir de um ASO.
        sst.MapPost("/exames-ocupacionais/{exameId:guid}/eventos/s2220", async (
            Guid exameId, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(new GerarS2220Command(exameId), ct) }))
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private static void MapearSstExposicoes(RouteGroupBuilder sst)
    {
        // S-2240 / PPP: inicia um periodo de exposicao a agentes nocivos.
        sst.MapPost("/exposicoes", async (
            RegistrarExposicaoAgenteNocivoCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Encerra um periodo de exposicao (gera o fim do periodo no PPP/S-2240).
        sst.MapPost("/exposicoes/{exposicaoId:guid}/encerramento", async (
            Guid exposicaoId, EncerrarExposicaoPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new EncerrarExposicaoAgenteNocivoCommand(exposicaoId, payload.FimExposicao), ct);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        // Registros ambientais (exposicoes) do servidor.
        sst.MapGet("/servidores/{servidorId:guid}/exposicoes", async (
            Guid servidorId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListarExposicoesQuery(servidorId), ct)))
            .RequirePermission("recursoshumanos.ver");

        // Gera o evento eSocial S-2240 (Condicoes Ambientais/Agentes Nocivos) a partir de uma exposicao.
        sst.MapPost("/exposicoes/{exposicaoId:guid}/eventos/s2240", async (
            Guid exposicaoId, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(new GerarS2240Command(exposicaoId), ct) }))
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private static void MapearSstCat(RouteGroupBuilder sst)
    {
        // CAT (S-2210): comunica um acidente de trabalho.
        sst.MapPost("/cat", async (
            ComunicarAcidenteCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // CAT do servidor.
        sst.MapGet("/servidores/{servidorId:guid}/cat", async (
            Guid servidorId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListarComunicacoesAcidenteQuery(servidorId), ct)))
            .RequirePermission("recursoshumanos.ver");

        // Gera o evento eSocial S-2210 (CAT) a partir de uma comunicacao de acidente.
        sst.MapPost("/cat/{catId:guid}/eventos/s2210", async (
            Guid catId, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(new GerarS2210Command(catId), ct) }))
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private static void MapearSstDocumentos(RouteGroupBuilder sst)
    {
        // PPP (Perfil Profissiografico Previdenciario) consolidado do servidor.
        sst.MapGet("/servidores/{servidorId:guid}/ppp", async (
            Guid servidorId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ObterPerfilProfissiograficoQuery(servidorId), ct)))
            .RequirePermission("recursoshumanos.ver");
    }

    private sealed record EncerrarExposicaoPayload(DateOnly FimExposicao);
}
