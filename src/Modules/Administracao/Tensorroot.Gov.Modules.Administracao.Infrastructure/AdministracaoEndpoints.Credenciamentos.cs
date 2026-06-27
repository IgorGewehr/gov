using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Administracao.Application.Credenciamentos;
using Tensorroot.Gov.Modules.Administracao.Domain.Credenciamentos;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure;

/// <summary>Endpoints HTTP do subdominio de CREDENCIAMENTO (Lei 14.133/2021, art. 78, I e art. 79).</summary>
internal static partial class AdministracaoEndpoints
{
    private static void MapCredenciamentos(RouteGroupBuilder grupo)
    {
        // === Edital de credenciamento ===
        grupo.MapPost("/credenciamentos", async (
            AbrirCredenciamentoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPost("/credenciamentos/{credenciamentoId:guid}/itens", async (
            Guid credenciamentoId, AdicionarItemCredenciamentoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                itemId = await sender.Send(
                    new AdicionarItemCredenciamentoCommand(credenciamentoId, payload.ItemCatalogoId, payload.Descricao, payload.UnidadeMedida, payload.PrecoFixado),
                    cancellationToken),
            }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapDelete("/credenciamentos/{credenciamentoId:guid}/itens/{itemId:guid}", async (
            Guid credenciamentoId, Guid itemId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RemoverItemCredenciamentoCommand(credenciamentoId, itemId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/credenciamentos/{credenciamentoId:guid}/publicar-chamamento", async (
            Guid credenciamentoId, NumeroEditalPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new PublicarChamamentoCommand(credenciamentoId, payload.NumeroEdital), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/credenciamentos/{credenciamentoId:guid}/chamamento", async (
            Guid credenciamentoId, AlterarChamamentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AlterarChamamentoCommand(credenciamentoId, payload.Suspender, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/credenciamentos/{credenciamentoId:guid}/encerrar", async (
            Guid credenciamentoId, EncerrarCredenciamentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EncerrarCredenciamentoCommand(credenciamentoId, payload.Situacao, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        // === Rol de credenciados (inscricoes a qualquer tempo — art. 79, par. unico) ===
        grupo.MapPost("/credenciamentos/{credenciamentoId:guid}/inscricoes", async (
            Guid credenciamentoId, InscreverInteressadoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                credenciadoId = await sender.Send(new InscreverInteressadoCommand(credenciamentoId, payload.FornecedorId), cancellationToken),
            }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPost("/credenciamentos/{credenciamentoId:guid}/inscricoes/{credenciadoId:guid}/deferir", async (
            Guid credenciamentoId, Guid credenciadoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DeferirInscricaoCommand(credenciamentoId, credenciadoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/credenciamentos/{credenciamentoId:guid}/inscricoes/{credenciadoId:guid}/indeferir", async (
            Guid credenciamentoId, Guid credenciadoId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new IndeferirInscricaoCommand(credenciamentoId, credenciadoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/credenciamentos/{credenciamentoId:guid}/credenciados/{credenciadoId:guid}/situacao", async (
            Guid credenciamentoId, Guid credenciadoId, AlterarCredenciadoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AlterarCredenciadoCommand(credenciamentoId, credenciadoId, payload.Suspender, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/credenciamentos/{credenciamentoId:guid}/credenciados/{credenciadoId:guid}/descredenciar", async (
            Guid credenciamentoId, Guid credenciadoId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DescredenciarCommand(credenciamentoId, credenciadoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        // === Consultas ===
        grupo.MapGet("/credenciamentos/{credenciamentoId:guid}", async (
            Guid credenciamentoId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterCredenciamentoPorIdQuery(credenciamentoId), cancellationToken) is { } detalhe
                ? Results.Ok(detalhe)
                : Results.NotFound())
            .RequirePermission("administracao.ver");

        grupo.MapGet("/credenciamentos", async (
            SituacaoCredenciamento? situacao, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarCredenciamentosQuery(situacao), cancellationToken)))
            .RequirePermission("administracao.ver");
    }

    private sealed record AdicionarItemCredenciamentoPayload(
        Guid? ItemCatalogoId,
        string Descricao,
        string UnidadeMedida,
        decimal PrecoFixado);

    private sealed record NumeroEditalPayload(string NumeroEdital);

    private sealed record AlterarChamamentoPayload(bool Suspender, string? Motivo);

    private sealed record EncerrarCredenciamentoPayload(SituacaoCredenciamento Situacao, string Motivo);

    private sealed record InscreverInteressadoPayload(Guid FornecedorId);

    private sealed record AlterarCredenciadoPayload(bool Suspender, string? Motivo);
}
