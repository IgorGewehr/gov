using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Tributos.Application.Iss;
using Tensorroot.Gov.Modules.Tributos.Application.Itbi;
using Tensorroot.Gov.Modules.Tributos.Application.Nfse;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) do ISS (apuração sobre NFS-e ingeridas — passiva) e do ITBI
/// (guia avulsa por transmissão). RBAC por permissões tributos.* (negar por padrão — CLAUDE.md §6).
/// </summary>
internal static class IssItbiEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/tributos").WithTags("Tributos.IssItbi");

        // ISS — ingestão (sob demanda) das NFS-e do ADN para o tenant atual. Integração PASSIVA
        // (ADR-0003 / CLAUDE.md §8): apenas baixamos/deduplicamos do Ambiente Nacional — não emitimos.
        // Espelha a lógica do Worker NfseSync; útil para gestão e para provar a ingestão por HTTP.
        grupo.MapPost("/iss/nfse/sincronizar", async (
            SincronizarNfsePayload payload, INfseSincronizador sincronizador, CancellationToken cancellationToken)
            => Results.Ok(new { importadas = await sincronizador.SincronizarAsync(payload.Prestadores, payload.Desde, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        // ISS — tabela de alíquotas por item LC 116 (lei municipal parametrizável).
        grupo.MapPost("/iss/aliquotas", async (
            ConfigurarTabelaAliquotaIssCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        // ISS — apuração mensal (livro eletrônico) de um contribuinte a partir das NFS-e ingeridas.
        grupo.MapPost("/iss/contribuintes/{contribuinteId:guid}/apurar", async (
            Guid contribuinteId, ApurarIssPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new ApurarIssMensalCommand(contribuinteId, payload.Ano, payload.Mes, payload.VencimentoIssProprio),
                cancellationToken)))
            .RequirePermission("tributos.gerenciar");

        // ITBI — alíquota por exercício (lei municipal parametrizável).
        grupo.MapPost("/itbi/aliquotas", async (
            ConfigurarAliquotaItbiCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        // ITBI — apuração (preview) de uma transmissão: base = maior(valor venal, declarado).
        grupo.MapGet("/itbi/imoveis/{imovelId:guid}/preview", async (
            Guid imovelId, int exercicio, decimal valorDeclarado, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new CalcularItbiQuery(imovelId, exercicio, valorDeclarado), cancellationToken)))
            .RequirePermission("tributos.ver");

        // ITBI — registra a transmissão, lança o imposto e gera a guia avulsa (DAM).
        grupo.MapPost("/itbi/lancar", async (
            LancarItbiCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(comando, cancellationToken)))
            .RequirePermission("tributos.gerenciar");
    }

    private sealed record ApurarIssPayload(int Ano, int Mes, DateOnly VencimentoIssProprio);

    private sealed record SincronizarNfsePayload(IReadOnlyList<string> Prestadores, DateOnly Desde);
}
