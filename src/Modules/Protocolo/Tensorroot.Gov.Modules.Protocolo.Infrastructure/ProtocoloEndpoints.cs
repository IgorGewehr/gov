using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Protocolo.Application.Arquivistica;
using Tensorroot.Gov.Modules.Protocolo.Application.Documentos;
using Tensorroot.Gov.Modules.Protocolo.Application.Processos;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do módulo Protocolo.</summary>
internal static class ProtocoloEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/protocolo").WithTags("Protocolo");

        MapearProcessos(grupo);
        MapearDocumentos(grupo);
        MapearTemporalidade(grupo);
    }

    private static void MapearTemporalidade(RouteGroupBuilder grupo)
    {
        // Cadastro do Plano de Classificacao e da TTD por tenant (Peca 2 / W9.4).
        grupo.MapPost("/planos-classificacao", async (
            CadastrarPlanoClassificacaoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("protocolo.gerenciar");

        grupo.MapPost("/tabelas-temporalidade", async (
            CadastrarTabelaTemporalidadeCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("protocolo.gerenciar");

        // Varredura de aptidao (promove AguardandoPrazo -> AptoEliminar quando o prazo decorreu).
        grupo.MapPost("/destinacoes/avaliar-aptidao", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { promovidas = await sender.Send(new AvaliarAptidaoDestinacoesCommand(), cancellationToken) }))
            .RequirePermission("protocolo.gerenciar");

        // Autorizacao de eliminacao por ato humano (RBAC) — I-T1/I-T2 protegem o irreversivel.
        grupo.MapPost("/destinacoes/{destinacaoId:guid}/autorizacao-eliminacao", async (
            Guid destinacaoId, AutorizarEliminacaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AutorizarEliminacaoCommand(destinacaoId, payload.AutorizadoPor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("protocolo.gerenciar");

        // Registro da eliminacao com termo assinado/carimbado + edital (I-T4 — prova oponivel ao TCE).
        grupo.MapPost("/destinacoes/{destinacaoId:guid}/eliminacao", async (
            Guid destinacaoId, RegistrarEliminacaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new RegistrarEliminacaoCommand(destinacaoId, payload.TermoEliminacaoHash, payload.EditalEliminacaoRef),
                cancellationToken);
            return Results.NoContent();
        }).RequirePermission("protocolo.gerenciar");
    }

    private static void MapearProcessos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/processos", async (
            AutuarProcessoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("protocolo.gerenciar");

        grupo.MapGet("/processos/{nup}", async (
            string nup, ISender sender, CancellationToken cancellationToken) =>
        {
            var processo = await sender.Send(new ObterProcessoPorNupQuery(nup), cancellationToken);
            return processo is null ? Results.NotFound() : Results.Ok(processo);
        }).RequirePermission("protocolo.ver");

        grupo.MapGet("/setores/{setorId:guid}/processos", async (
            Guid setorId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarProcessosDoSetorQuery(setorId), cancellationToken)))
            .RequirePermission("protocolo.ver");

        grupo.MapPost("/processos/{processoId:guid}/tramitacoes", async (
            Guid processoId, TramitarProcessoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new TramitarProcessoCommand(processoId, payload.SetorDestinoId, payload.Observacao), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("protocolo.gerenciar");

        grupo.MapPost("/processos/{processoId:guid}/despachos", async (
            Guid processoId, DespacharProcessoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DespacharProcessoCommand(processoId, payload.Texto, payload.AutoridadeId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("protocolo.gerenciar");

        grupo.MapPost("/processos/{processoId:guid}/sobrestamento", async (
            Guid processoId, SobrestarProcessoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new SobrestarProcessoCommand(processoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("protocolo.gerenciar");

        grupo.MapPost("/processos/{processoId:guid}/arquivamento", async (
            Guid processoId, ArquivarProcessoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ArquivarProcessoCommand(processoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("protocolo.gerenciar");
    }

    private static void MapearDocumentos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/documentos", async (
            JuntarDocumentoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("protocolo.gerenciar");

        grupo.MapGet("/processos/{processoId:guid}/documentos", async (
            Guid processoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarDocumentosDoProcessoQuery(processoId), cancellationToken)))
            .RequirePermission("protocolo.ver");

        grupo.MapPost("/documentos/{documentoId:guid}/assinaturas", async (
            Guid documentoId, AssinarDocumentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AssinarDocumentoCommand(documentoId, payload.SignatarioId, payload.Tipo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("protocolo.gerenciar");

        grupo.MapPost("/documentos/{documentoId:guid}/sem-efeito", async (
            Guid documentoId, TornarSemEfeitoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new TornarDocumentoSemEfeitoCommand(documentoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("protocolo.gerenciar");

        grupo.MapGet("/documentos/{documentoId:guid}/integridade", async (
            Guid documentoId, string hashRecalculado, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { integro = await sender.Send(new VerificarIntegridadeDocumentoQuery(documentoId, hashRecalculado), cancellationToken) }))
            .RequirePermission("protocolo.ver");
    }

    private sealed record TramitarProcessoPayload(Guid SetorDestinoId, string? Observacao);

    private sealed record DespacharProcessoPayload(string Texto, Guid AutoridadeId);

    private sealed record SobrestarProcessoPayload(string Motivo);

    private sealed record ArquivarProcessoPayload(string? Motivo);

    private sealed record AssinarDocumentoPayload(Guid SignatarioId, Domain.Documentos.TipoAssinatura Tipo);

    private sealed record TornarSemEfeitoPayload(string Motivo);

    private sealed record AutorizarEliminacaoPayload(Guid AutorizadoPor);

    private sealed record RegistrarEliminacaoPayload(string TermoEliminacaoHash, string EditalEliminacaoRef);
}
