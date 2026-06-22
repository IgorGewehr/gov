using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Commands;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Demonstracoes;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Msc;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Queries;
using Tensorroot.Gov.Modules.Financas.Application.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Application.Empenhos;
using Tensorroot.Gov.Modules.Financas.Application.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Application.Pagamentos;
using Tensorroot.Gov.Modules.Financas.Application.RestosAPagar;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) do módulo Finanças — ciclo da despesa pública orçamentária
/// (Lei 4.320/64): dotação → empenho → liquidação → pagamento → restos a pagar.
/// Leituras exigem "financas.ver"; mutações exigem "financas.gerenciar".
/// </summary>
internal static class FinancasEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/financas").WithTags("Financas");

        MapearDotacoes(grupo);
        MapearEmpenhos(grupo);
        MapearLiquidacoes(grupo);
        MapearPagamentos(grupo);
        MapearRestosAPagar(grupo);
        MapearContabilidade(grupo);
    }

    private static void MapearContabilidade(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/contabilidade/plano-de-contas/semear", async (ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { criadas = await sender.Send(new SemearPlanoDeContasCommand(), cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        grupo.MapGet("/contabilidade/plano-de-contas", async (ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarPlanoDeContasQuery(), cancellationToken)))
            .RequirePermission("financas.ver");

        grupo.MapPost("/contabilidade/lancamentos", async (
            RegistrarLancamentoManualCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        grupo.MapGet("/contabilidade/balancete", async (
            int exercicio, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ConsultarBalanceteQuery(exercicio, mes), cancellationToken)))
            .RequirePermission("financas.ver");

        grupo.MapGet("/contabilidade/contas/{contaId:guid}/razao", async (
            Guid contaId, int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ConsultarRazaoQuery(contaId, exercicio), cancellationToken)))
            .RequirePermission("financas.ver");

        MapearMscEDemonstracoes(grupo);
    }

    private static void MapearMscEDemonstracoes(RouteGroupBuilder grupo)
    {
        // Geração da MSC (deriva do balancete e publica o MSCGeradaIntegrationEvent via Outbox).
        grupo.MapPost("/contabilidade/msc/gerar", async (
            GerarMscPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new GerarMscCommand(payload.Exercicio, payload.Mes, payload.PoderOrgao), cancellationToken)))
            .RequirePermission("financas.gerenciar");

        // Demonstrações DCASP (read-models derivados do balancete).
        grupo.MapGet("/contabilidade/demonstracoes/balanco-orcamentario", async (
            int exercicio, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new GerarBalancoOrcamentarioQuery(exercicio, mes), cancellationToken)))
            .RequirePermission("financas.ver");

        grupo.MapGet("/contabilidade/demonstracoes/balanco-financeiro", async (
            int exercicio, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new GerarBalancoFinanceiroQuery(exercicio, mes), cancellationToken)))
            .RequirePermission("financas.ver");

        grupo.MapGet("/contabilidade/demonstracoes/balanco-patrimonial", async (
            int exercicio, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new GerarBalancoPatrimonialQuery(exercicio, mes), cancellationToken)))
            .RequirePermission("financas.ver");

        grupo.MapGet("/contabilidade/demonstracoes/variacoes-patrimoniais", async (
            int exercicio, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new GerarDvpQuery(exercicio, mes), cancellationToken)))
            .RequirePermission("financas.ver");
    }

    private static void MapearDotacoes(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/dotacoes", async (CriarDotacaoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        grupo.MapPost("/dotacoes/{dotacaoId:guid}/reforcar", async (
            Guid dotacaoId, ReforcarDotacaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ReforcarDotacaoCommand(dotacaoId, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapPost("/dotacoes/{dotacaoId:guid}/anular-credito", async (
            Guid dotacaoId, AnularCreditoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AnularCreditoDotacaoCommand(dotacaoId, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapGet("/dotacoes/{dotacaoId:guid}", async (Guid dotacaoId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterDotacaoQuery(dotacaoId), cancellationToken) is { } resumo
                ? Results.Ok(resumo)
                : Results.NotFound())
            .RequirePermission("financas.ver");

        grupo.MapGet("/dotacoes", async (int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarDotacoesPorExercicioQuery(exercicio), cancellationToken)))
            .RequirePermission("financas.ver");
    }

    private static void MapearEmpenhos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/empenhos", async (EmpenharCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        grupo.MapPost("/empenhos/{empenhoId:guid}/anular", async (
            Guid empenhoId, AnularEmpenhoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AnularEmpenhoCommand(empenhoId, payload?.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapGet("/empenhos/{empenhoId:guid}", async (Guid empenhoId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterEmpenhoQuery(empenhoId), cancellationToken) is { } resumo
                ? Results.Ok(resumo)
                : Results.NotFound())
            .RequirePermission("financas.ver");

        grupo.MapGet("/dotacoes/{dotacaoId:guid}/empenhos", async (Guid dotacaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarEmpenhosPorDotacaoQuery(dotacaoId), cancellationToken)))
            .RequirePermission("financas.ver");
    }

    private static void MapearLiquidacoes(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/liquidacoes", async (LiquidarDespesaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        grupo.MapPost("/liquidacoes/{liquidacaoId:guid}/estornar", async (
            Guid liquidacaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EstornarLiquidacaoCommand(liquidacaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapGet("/liquidacoes/{liquidacaoId:guid}", async (Guid liquidacaoId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterLiquidacaoQuery(liquidacaoId), cancellationToken) is { } resumo
                ? Results.Ok(resumo)
                : Results.NotFound())
            .RequirePermission("financas.ver");

        grupo.MapGet("/empenhos/{empenhoId:guid}/liquidacoes", async (Guid empenhoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarLiquidacoesPorEmpenhoQuery(empenhoId), cancellationToken)))
            .RequirePermission("financas.ver");
    }

    private static void MapearPagamentos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/ordens-pagamento", async (EmitirOrdemDePagamentoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        grupo.MapPost("/ordens-pagamento/{ordemId:guid}/efetuar", async (
            Guid ordemId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EfetuarPagamentoCommand(ordemId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapPost("/ordens-pagamento/{ordemId:guid}/cancelar", async (
            Guid ordemId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarOrdemDePagamentoCommand(ordemId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapGet("/ordens-pagamento/{ordemId:guid}", async (Guid ordemId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterOrdemDePagamentoQuery(ordemId), cancellationToken) is { } resumo
                ? Results.Ok(resumo)
                : Results.NotFound())
            .RequirePermission("financas.ver");
    }

    private static void MapearRestosAPagar(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/restos-a-pagar/encerrar-exercicio", async (
            EncerrarExercicioPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { inscritos = await sender.Send(new EncerrarExercicioCommand(payload.Exercicio), cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        grupo.MapPost("/restos-a-pagar/{restoId:guid}/pagar", async (
            Guid restoId, ValorPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new PagarRestoAPagarCommand(restoId, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapPost("/restos-a-pagar/{restoId:guid}/cancelar", async (
            Guid restoId, ValorPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarRestoAPagarCommand(restoId, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapGet("/restos-a-pagar", async (int exercicioInscricao, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarRestosAPagarPorExercicioQuery(exercicioInscricao), cancellationToken)))
            .RequirePermission("financas.ver");
    }

    private sealed record ReforcarDotacaoPayload(decimal Valor);

    private sealed record AnularCreditoPayload(decimal Valor);

    private sealed record AnularEmpenhoPayload(decimal? Valor);

    private sealed record EncerrarExercicioPayload(int Exercicio);

    private sealed record ValorPayload(decimal Valor);

    private sealed record GerarMscPayload(int Exercicio, int Mes, string? PoderOrgao);
}
