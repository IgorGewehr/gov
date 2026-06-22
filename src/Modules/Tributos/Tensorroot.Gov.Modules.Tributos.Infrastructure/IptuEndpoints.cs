using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Tributos.Application.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Application.Iptu;
using Tensorroot.Gov.Modules.Tributos.Application.Pgv;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) do IPTU: cadastro imobiliário, PGV, tabela de alíquotas, apuração
/// e lançamento anual. RBAC por permissões tributos.* (negar por padrão — CLAUDE.md §6).
/// </summary>
internal static class IptuEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/tributos").WithTags("Tributos.Iptu");

        // Cadastro imobiliário (BCI).
        grupo.MapPost("/imoveis", async (
            CadastrarImovelCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        grupo.MapGet("/contribuintes/{contribuinteId:guid}/imoveis", async (
            Guid contribuinteId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarImoveisDoContribuinteQuery(contribuinteId), cancellationToken)))
            .RequirePermission("tributos.ver");

        // PGV (Planta Genérica de Valores) — lei municipal parametrizável.
        grupo.MapPost("/pgv", async (
            ConfigurarPlantaValoresCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        // Tabela de alíquotas do IPTU — lei municipal parametrizável.
        grupo.MapPost("/iptu/aliquotas", async (
            ConfigurarTabelaAliquotaIptuCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        // Apuração (preview) do IPTU de um imóvel num exercício.
        grupo.MapGet("/imoveis/{imovelId:guid}/iptu/{exercicio:int}", async (
            Guid imovelId, int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new CalcularIptuQuery(imovelId, exercicio), cancellationToken)))
            .RequirePermission("tributos.ver");

        // Lançamento anual de ofício do IPTU + geração do DAM.
        grupo.MapPost("/imoveis/{imovelId:guid}/iptu/lancar", async (
            Guid imovelId, LancarIptuPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new LancarIptuAnualCommand(
                    imovelId,
                    payload.Exercicio,
                    payload.PrimeiroVencimento,
                    payload.NumeroParcelas,
                    payload.PercentualIsencao,
                    payload.PercentualDesconto),
                cancellationToken)))
            .RequirePermission("tributos.gerenciar");
    }

    private sealed record LancarIptuPayload(
        int Exercicio,
        DateOnly PrimeiroVencimento,
        int NumeroParcelas = 1,
        decimal PercentualIsencao = 0m,
        decimal PercentualDesconto = 0m);
}
