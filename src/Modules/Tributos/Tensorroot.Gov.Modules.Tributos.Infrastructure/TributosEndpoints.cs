using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Tributos.Application.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Application.Dividas;
using Tensorroot.Gov.Modules.Tributos.Application.Lancamentos;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do módulo Tributos.</summary>
internal static class TributosEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/tributos").WithTags("Tributos");

        grupo.MapPost("/contribuintes/pessoa-fisica", async (
            CadastrarContribuintePessoaFisicaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        grupo.MapPost("/lancamentos", async (
            LancarCreditoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        grupo.MapPost("/lancamentos/{lancamentoId:guid}/inscrever-divida-ativa", async (
            Guid lancamentoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { dividaAtivaId = await sender.Send(new InscreverEmDividaAtivaCommand(lancamentoId), cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        grupo.MapPost("/dividas/{dividaAtivaId:guid}/cda", async (
            Guid dividaAtivaId, EmitirCdaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EmitirCdaCommand(dividaAtivaId, payload.NumeroCda), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("tributos.gerenciar");

        grupo.MapGet("/contribuintes/{contribuinteId:guid}/dividas-ativas", async (
            Guid contribuinteId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterDividasAtivasDoContribuinteQuery(contribuinteId), cancellationToken)))
            .RequirePermission("tributos.ver");
    }

    private sealed record EmitirCdaPayload(string NumeroCda);
}
