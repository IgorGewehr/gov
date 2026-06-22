using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Educacao.Application.DiarioClasse;
using Tensorroot.Gov.Modules.Educacao.Application.Escolas;
using Tensorroot.Gov.Modules.Educacao.Application.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do modulo Educacao.</summary>
internal static class EducacaoEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/educacao").WithTags("Educacao");

        MapearEscolas(grupo);
        MapearMatriculas(grupo);
        MapearDiarios(grupo);
    }

    private static void MapearEscolas(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/escolas", async (
            CredenciarEscolaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapGet("/escolas", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarEscolasDaRedeQuery(), cancellationToken)))
            .RequirePermission("educacao.ver");

        grupo.MapGet("/escolas/{codigoInep}", async (
            string codigoInep, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterEscolaPorCodigoInepQuery(codigoInep), cancellationToken)))
            .RequirePermission("educacao.ver");

        grupo.MapPut("/escolas/{escolaId:guid}/dados-censo", async (
            Guid escolaId, AtualizarDadosCensoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AtualizarDadosCensoCommand(escolaId, payload.Endereco, payload.Infraestrutura), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/escolas/{escolaId:guid}/desativacao", async (
            Guid escolaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DesativarEscolaCommand(escolaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");
    }

    private static void MapearMatriculas(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/matriculas", async (
            MatricularAlunoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapPost("/matriculas/rematricula", async (
            RematricularAlunoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapPost("/matriculas/{matriculaId:guid}/transferencia", async (
            Guid matriculaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new TransferirAlunoCommand(matriculaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/matriculas/{matriculaId:guid}/encerramento", async (
            Guid matriculaId, EncerrarMatriculaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EncerrarMatriculaCommand(matriculaId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/matriculas/{matriculaId:guid}/situacao-aluno", async (
            Guid matriculaId, RegistrarSituacaoDoAlunoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarSituacaoDoAlunoCommand(matriculaId, payload.Rendimento, payload.Movimento), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapGet("/alunos/{alunoId:guid}/matriculas", async (
            Guid alunoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterMatriculasDoAlunoQuery(alunoId), cancellationToken)))
            .RequirePermission("educacao.ver");

        grupo.MapGet("/turmas/{turmaId:guid}/matricula-inicial", async (
            Guid turmaId, DateOnly dataReferencia, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarMatriculasInicialDaTurmaQuery(turmaId, dataReferencia), cancellationToken)))
            .RequirePermission("educacao.ver");
    }

    private static void MapearDiarios(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/diarios", async (
            AbrirDiarioClasseCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("educacao.gerenciar");

        grupo.MapPost("/diarios/{diarioId:guid}/frequencias", async (
            Guid diarioId, RegistrarFrequenciaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarFrequenciaCommand(diarioId, payload.Data, payload.Presente, payload.CargaHorariaAula), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/diarios/{diarioId:guid}/notas", async (
            Guid diarioId, LancarNotaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new LancarNotaCommand(diarioId, payload.ComponenteCurricularId, payload.Periodo, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/diarios/{diarioId:guid}/aulas", async (
            Guid diarioId, RegistrarAulaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarAulaCommand(diarioId, payload.Data, payload.Conteudo, payload.DiaLetivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapPost("/diarios/{diarioId:guid}/apuracao", async (
            Guid diarioId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ApurarResultadoCommand(diarioId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("educacao.gerenciar");

        grupo.MapGet("/diarios/{diarioId:guid}/frequencia", async (
            Guid diarioId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterFrequenciaDoDiarioQuery(diarioId), cancellationToken)))
            .RequirePermission("educacao.ver");

        grupo.MapGet("/matriculas/{matriculaId:guid}/diario", async (
            Guid matriculaId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterDiarioDaMatriculaQuery(matriculaId), cancellationToken)))
            .RequirePermission("educacao.ver");
    }

    private sealed record AtualizarDadosCensoPayload(
        Domain.ValueObjects.Endereco Endereco,
        Domain.ValueObjects.Infraestrutura Infraestrutura);

    private sealed record EncerrarMatriculaPayload(MotivoEncerramento Motivo);

    private sealed record RegistrarSituacaoDoAlunoPayload(Rendimento Rendimento, Movimento Movimento);

    private sealed record RegistrarFrequenciaPayload(DateOnly Data, bool Presente, int CargaHorariaAula);

    private sealed record LancarNotaPayload(Guid ComponenteCurricularId, string Periodo, decimal Valor);

    private sealed record RegistrarAulaPayload(DateOnly Data, string Conteudo, bool DiaLetivo);
}
