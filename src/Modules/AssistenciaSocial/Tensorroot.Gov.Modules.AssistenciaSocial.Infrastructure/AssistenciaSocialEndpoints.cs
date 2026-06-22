using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Beneficios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Prontuarios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do modulo AssistenciaSocial.</summary>
internal static class AssistenciaSocialEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/assistenciasocial").WithTags("AssistenciaSocial");

        MapearFamilias(grupo);
        MapearBeneficios(grupo);
        MapearProntuarios(grupo);
    }

    private static void MapearFamilias(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/familias", async (
            ReferenciarFamiliaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("assistenciasocial.gerenciar");

        grupo.MapPut("/familias/{familiaId:guid}/renda", async (
            Guid familiaId, AtualizarRendaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AtualizarRendaFamiliarCommand(familiaId, payload.Membros), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        grupo.MapPost("/familias/{familiaId:guid}/vigencia-cadastral", async (
            Guid familiaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ProcessarVigenciaCadastralCommand(familiaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        grupo.MapGet("/familias", async (
            string territorio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterFamiliasDoTerritorioQuery(territorio), cancellationToken)))
            .RequirePermission("assistenciasocial.ver");

        grupo.MapGet("/familias/{familiaId:guid}/cadunico", async (
            Guid familiaId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterResumoCadUnicoQuery(familiaId), cancellationToken)))
            .RequirePermission("assistenciasocial.ver");
    }

    private static void MapearBeneficios(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/beneficios/elegibilidade", async (
            AvaliarElegibilidadeBeneficioCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("assistenciasocial.gerenciar");

        grupo.MapPost("/beneficios/{beneficioId:guid}/cesta-basica", async (
            Guid beneficioId, EntregarCestaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EntregarCestaBasicaCommand(beneficioId, payload.Quantidade), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        grupo.MapGet("/familias/{familiaId:guid}/beneficios", async (
            Guid familiaId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterBeneficiosDaFamiliaQuery(familiaId), cancellationToken)))
            .RequirePermission("assistenciasocial.ver");

        grupo.MapGet("/beneficios/concessoes", async (
            int ano, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterConcessoesPorCompetenciaQuery(Competencia.De(ano, mes)), cancellationToken)))
            .RequirePermission("assistenciasocial.ver");
    }

    private static void MapearProntuarios(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/prontuarios", async (
            AbrirProntuarioCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("assistenciasocial.gerenciar");

        grupo.MapPost("/prontuarios/{prontuarioId:guid}/atendimentos", async (
            Guid prontuarioId, RegistrarAtendimentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarAtendimentoCommand(
                prontuarioId, payload.Servico, payload.DataAtendimento, payload.Descricao, payload.ProfissionalId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        grupo.MapPost("/prontuarios/{prontuarioId:guid}/encerramento", async (
            Guid prontuarioId, EncerrarPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EncerrarAcompanhamentoCommand(prontuarioId, payload.MotivoEncerramento), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        // LG-1: o usuario do acesso e SEMPRE o principal autenticado (claim 'sub'), derivado no
        // handler — nunca um valor da query string/body do cliente. So o motivo e entrada validada.
        grupo.MapPost("/prontuarios/{prontuarioId:guid}/acessos", async (
            Guid prontuarioId, RegistrarAcessoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarAcessoProntuarioCommand(prontuarioId, payload.MotivoAcesso), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        grupo.MapGet("/familias/{familiaId:guid}/prontuario", async (
            Guid familiaId, string motivoAcesso, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterProntuarioDaFamiliaQuery(familiaId, motivoAcesso), cancellationToken)))
            .RequirePermission("assistenciasocial.ver");

        grupo.MapGet("/prontuarios/{prontuarioId:guid}/trilha-acesso", async (
            Guid prontuarioId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterTrilhaAcessoProntuarioQuery(prontuarioId), cancellationToken)))
            .RequirePermission("assistenciasocial.ver");
    }

    private sealed record AtualizarRendaPayload(IReadOnlyList<MembroFamiliarDto> Membros);

    private sealed record EntregarCestaPayload(int Quantidade);

    private sealed record RegistrarAtendimentoPayload(
        Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios.TipoServico Servico,
        DateOnly DataAtendimento,
        string Descricao,
        Guid ProfissionalId);

    private sealed record EncerrarPayload(string MotivoEncerramento);

    // LG-1: o UsuarioId NAO faz parte do payload — e derivado do principal autenticado no handler.
    private sealed record RegistrarAcessoPayload(string MotivoAcesso);
}
