using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do modulo RecursosHumanos.</summary>
internal static class RecursosHumanosEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/recursoshumanos").WithTags("RecursosHumanos");

        MapearServidores(grupo);
        MapearCargos(grupo);
        MapearFolha(grupo);
    }

    private static void MapearServidores(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/servidores", async (
            AdmitirServidorCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapGet("/servidores/ativos", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarServidoresAtivosQuery(), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        grupo.MapGet("/servidores/por-matricula/{matricula}", async (
            string matricula, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterServidorPorMatriculaQuery(matricula), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        grupo.MapPost("/servidores/{servidorId:guid}/posse", async (
            Guid servidorId, PossePayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarPosseCommand(servidorId, payload.DataPosse), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/servidores/{servidorId:guid}/exercicio", async (
            Guid servidorId, ExercicioPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new IniciarExercicioCommand(servidorId, payload.DataExercicio), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/servidores/{servidorId:guid}/estabilidade", async (
            Guid servidorId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ConcederEstabilidadeCommand(servidorId), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/servidores/{servidorId:guid}/afastamento", async (
            Guid servidorId, AfastamentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarAfastamentoCommand(servidorId, payload.Inicio, payload.Fim, payload.Motivo), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/servidores/{servidorId:guid}/desligamento", async (
            Guid servidorId, DesligamentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DesligarServidorCommand(servidorId, payload.DataDesligamento, payload.Motivo), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private static void MapearCargos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/cargos", async (
            CriarCargoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapGet("/cargos/{cargoId:guid}", async (
            Guid cargoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterCargoPorIdQuery(cargoId), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        grupo.MapGet("/cargos/com-vagas", async (
            TipoCargo? tipo, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarCargosComVagasQuery(tipo), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        grupo.MapPost("/cargos/{cargoId:guid}/provimento", async (
            Guid cargoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ProverCargoCommand(cargoId), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/cargos/{cargoId:guid}/vacancia", async (
            Guid cargoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new VagarCargoCommand(cargoId), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/cargos/{cargoId:guid}/vencimento", async (
            Guid cargoId, AlterarVencimentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AlterarVencimentoCommand(cargoId, payload.NovoVencimento), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/cargos/{cargoId:guid}/extincao", async (
            Guid cargoId, ExtinguirCargoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ExtinguirCargoCommand(cargoId, payload.LeiExtincao), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private static void MapearFolha(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/folhas", async (
            AbrirFolhaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapGet("/folhas/por-competencia", async (
            int ano, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterFolhaPorCompetenciaQuery(ano, mes), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        grupo.MapPost("/folhas/{folhaId:guid}/eventos", async (
            Guid folhaId, EventoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AdicionarEventoCommand(
                folhaId, payload.ServidorId, payload.Rubrica, payload.Tipo, payload.BaseCalculo, payload.Valor), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/folhas/{folhaId:guid}/calculo", async (
            Guid folhaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CalcularFolhaCommand(folhaId), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/folhas/{folhaId:guid}/fechamento", async (
            Guid folhaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new FecharFolhaCommand(folhaId), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapPost("/folhas/{folhaId:guid}/pagamento", async (
            Guid folhaId, PagamentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EfetuarPagamentoCommand(folhaId, payload.DataPagamento), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        grupo.MapGet("/folhas/{folhaId:guid}/servidores/{servidorId:guid}/contracheque", async (
            Guid folhaId, Guid servidorId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterContrachequeDoServidorQuery(folhaId, servidorId), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");
    }

    private sealed record PossePayload(DateOnly DataPosse);

    private sealed record ExercicioPayload(DateOnly DataExercicio);

    private sealed record AfastamentoPayload(DateOnly Inicio, DateOnly? Fim, string Motivo);

    private sealed record DesligamentoPayload(DateOnly DataDesligamento, string Motivo);

    private sealed record AlterarVencimentoPayload(decimal NovoVencimento);

    private sealed record ExtinguirCargoPayload(string LeiExtincao);

    private sealed record EventoPayload(Guid ServidorId, string Rubrica, TipoEvento Tipo, decimal BaseCalculo, decimal Valor);

    private sealed record PagamentoPayload(DateOnly DataPagamento);
}
