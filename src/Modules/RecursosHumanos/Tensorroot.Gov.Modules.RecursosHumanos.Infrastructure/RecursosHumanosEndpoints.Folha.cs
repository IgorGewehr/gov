using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;

/// <summary>Endpoints HTTP da folha de pagamento (abrir, eventos, calculo, conferencia P0-7, fechamento, pagamento).</summary>
internal static partial class RecursosHumanosEndpoints
{
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

        // Apura INSS/RPPS/IRRF por servidor (motor + tabelas parametrizadas) com a folha ainda aberta.
        grupo.MapPost("/folhas/{folhaId:guid}/apuracao-legal", async (
            Guid folhaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ApurarDescontosLegaisCommand(folhaId), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        // P0-2: consolida INSS/IRRF de TODAS as folhas mensais (Mensal + Ferias + ...) da competencia sobre
        // a base SOMADA — teto INSS unico e faixa IRRF progressiva. Concentra o desconto na folha principal.
        grupo.MapPost("/folhas/consolidacao-legal", async (
            int ano, int mes, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ConsolidarDescontosLegaisMensaisCommand(ano, mes), cancellationToken);
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

        // P0-5: confirmarLiquidoInsuficiente=true e a confirmacao EXPLICITA do operador (revisao feita) p/ fechar folha com liquido insuficiente; default false (recusa).
        grupo.MapPost("/folhas/{folhaId:guid}/fechamento", async (
            Guid folhaId, ISender sender, CancellationToken cancellationToken, bool confirmarLiquidoInsuficiente = false) =>
        {
            await sender.Send(new FecharFolhaCommand(folhaId, confirmarLiquidoInsuficiente), cancellationToken);
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

        // P0-7: conferencia de pre-fechamento — totais (geral + por rubrica) e divergencias (ativos sem
        // lancamento, liquido insuficiente, variacao suspeita vs competencia anterior, totais batendo) que
        // o conferente precisa ver ANTES de fechar. So leitura; limiteVariacaoLiquido sobrescreve o default
        // do tenant (parametrizavel). 404 (corpo nulo) quando a folha nao existe no tenant.
        grupo.MapGet("/folhas/{folhaId:guid}/conferencia", async (
            Guid folhaId, ISender sender, CancellationToken cancellationToken, decimal? limiteVariacaoLiquido = null)
            => Results.Ok(await sender.Send(new ObterConferenciaFolhaQuery(folhaId, limiteVariacaoLiquido), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");
    }

    private sealed record EventoPayload(Guid ServidorId, string Rubrica, TipoEvento Tipo, decimal BaseCalculo, decimal Valor);

    private sealed record PagamentoPayload(DateOnly DataPagamento);
}
