using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.Modules.Administracao.Application.Contratos;
using Tensorroot.Gov.Modules.Administracao.Application.Fornecedores;
using Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do modulo Administracao (Lei 14.133/2021).</summary>
internal static class AdministracaoEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/administracao").WithTags("Administracao");

        MapLicitacoes(grupo);
        MapContratos(grupo);
        MapFornecedores(grupo);
    }

    private static void MapLicitacoes(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/licitacoes", async (
            AbrirLicitacaoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPost("/licitacoes/{licitacaoId:guid}/edital-pncp", async (
            Guid licitacaoId, PublicarEditalPncpPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new PublicarEditalNoPncpCommand(licitacaoId, payload.NumeroEditalPncp), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/licitacoes/{licitacaoId:guid}/julgar", async (
            Guid licitacaoId, JulgarPropostasPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new JulgarPropostasCommand(licitacaoId, payload.PropostaVencedoraId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/licitacoes/{licitacaoId:guid}/homologar", async (
            Guid licitacaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new HomologarLicitacaoCommand(licitacaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapGet("/licitacoes/{licitacaoId:guid}", async (
            Guid licitacaoId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterLicitacaoPorIdQuery(licitacaoId), cancellationToken) is { } detalhe
                ? Results.Ok(detalhe)
                : Results.NotFound())
            .RequirePermission("administracao.ver");

        grupo.MapGet("/licitacoes", async (
            SituacaoLicitacao situacao, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarLicitacoesPorSituacaoQuery(situacao), cancellationToken)))
            .RequirePermission("administracao.ver");
    }

    private static void MapContratos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/contratos", async (
            CelebrarContratoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPost("/contratos/{contratoId:guid}/contrato-pncp", async (
            Guid contratoId, PublicarContratoPncpPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new PublicarContratoNoPncpCommand(contratoId, payload.NumeroContratoPncp), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/contratos/{contratoId:guid}/iniciar-execucao", async (
            Guid contratoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new IniciarExecucaoContratoCommand(contratoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/contratos/{contratoId:guid}/encerrar", async (
            Guid contratoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EncerrarContratoCommand(contratoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapGet("/contratos/{contratoId:guid}", async (
            Guid contratoId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterContratoPorIdQuery(contratoId), cancellationToken) is { } detalhe
                ? Results.Ok(detalhe)
                : Results.NotFound())
            .RequirePermission("administracao.ver");

        grupo.MapGet("/contratos/vigentes", async (
            DateOnly referencia, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarContratosVigentesQuery(referencia), cancellationToken)))
            .RequirePermission("administracao.ver");

        grupo.MapGet("/fornecedores/{fornecedorId:guid}/contratos", async (
            Guid fornecedorId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarContratosPorFornecedorQuery(fornecedorId), cancellationToken)))
            .RequirePermission("administracao.ver");
    }

    private static void MapFornecedores(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/fornecedores", async (
            CadastrarFornecedorCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPost("/fornecedores/{fornecedorId:guid}/sancoes", async (
            Guid fornecedorId, AplicarSancaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            var id = await sender.Send(
                new AplicarSancaoCommand(
                    fornecedorId,
                    payload.Tipo,
                    payload.DataInicio,
                    payload.DataFim,
                    payload.ProcessoAdministrativo,
                    payload.Fundamentacao,
                    payload.ValorMulta),
                cancellationToken);
            return Results.Ok(new { sancaoId = id });
        }).RequirePermission("administracao.gerenciar");

        grupo.MapGet("/fornecedores/{fornecedorId:guid}", async (
            Guid fornecedorId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterFornecedorPorIdQuery(fornecedorId), cancellationToken) is { } detalhe
                ? Results.Ok(detalhe)
                : Results.NotFound())
            .RequirePermission("administracao.ver");

        grupo.MapGet("/fornecedores/impedidos", async (
            DateOnly referencia, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarFornecedoresImpedidosQuery(referencia), cancellationToken)))
            .RequirePermission("administracao.ver");
    }

    private sealed record PublicarEditalPncpPayload(string NumeroEditalPncp);

    private sealed record JulgarPropostasPayload(Guid PropostaVencedoraId);

    private sealed record PublicarContratoPncpPayload(string NumeroContratoPncp);

    private sealed record AplicarSancaoPayload(
        Domain.Fornecedores.TipoSancao Tipo,
        DateOnly DataInicio,
        DateOnly? DataFim,
        string ProcessoAdministrativo,
        string Fundamentacao,
        decimal? ValorMulta);
}
