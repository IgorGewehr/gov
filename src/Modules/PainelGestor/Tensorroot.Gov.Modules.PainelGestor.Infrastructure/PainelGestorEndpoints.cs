using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.PainelGestor.Application.Indicadores;

namespace Tensorroot.Gov.Modules.PainelGestor.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) do Painel do Gestor + BI (M8). Todos são GET (read-only) gated pela
/// permissão de gestor <c>painel.ver</c> e retornam os indicadores por exercício, reprodutíveis. A rota
/// base <c>/api/painelgestor</c> casa com o nome do módulo para o gating de licenciamento por tenant do
/// ApiHost.
/// </summary>
internal static class PainelGestorEndpoints
{
    // Permissao do catalogo canonico (Identidade.Domain.Permissoes.PainelVer), referenciada como string
    // para nao acoplar a Infrastructure ao Domain de outro modulo (mesma convencao dos demais modulos).
    private const string PermissaoPainelVer = "painel.ver";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/painelgestor").WithTags("PainelGestor");

        // Painel consolidado do gestor: os 5 KPIs do exercício (execução, mínimos, arrecadação/dívida,
        // pessoal/% RCL-LRF, prontidão TCE). Reprodutível e tenant-scoped.
        grupo.MapGet("/indicadores/{exercicio:int}", async (
                int exercicio, ISender sender, CancellationToken cancellationToken)
                => Results.Ok(await sender.Send(new ObterPainelGestorQuery(exercicio), cancellationToken)))
            .RequirePermission(PermissaoPainelVer)
            .WithName("ObterPainelGestor")
            .WithSummary("Indicadores consolidados do gestor por exercício (execução, mínimos, arrecadação, pessoal/LRF, prestação de contas).");
    }
}
