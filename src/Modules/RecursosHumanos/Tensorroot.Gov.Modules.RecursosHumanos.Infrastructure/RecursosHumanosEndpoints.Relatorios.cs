using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Relatorios;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;

/// <summary>
/// Endpoints HTTP dos RELATORIOS gerenciais da folha (Onda 3a — ONDA3-DESIGN §4.1): folha por
/// secretaria/UO e fonte, evolucao mensal da despesa de pessoal, mapa de cargos e demonstrativo TCE.
/// Somente leitura (GET), gated por <c>recursoshumanos.ver</c>. 404 (corpo nulo) quando nao ha folha
/// mensal na competencia consultada.
/// </summary>
internal static partial class RecursosHumanosEndpoints
{
    private static void MapearRelatorios(RouteGroupBuilder grupo)
    {
        var relatorios = grupo.MapGroup("/relatorios").WithTags("RecursosHumanos.Relatorios");

        // Folha consolidada por secretaria/UO e por fonte (regime RPPS/RGPS) numa competencia.
        relatorios.MapGet("/folha-por-secretaria", async (
            int ano, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterFolhaPorSecretariaQuery(ano, mes), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        // Evolucao mensal da despesa de pessoal entre duas competencias (serie + acumulado/media);
        // base gerencial para o acompanhamento do limite da LRF (art. 19/20).
        relatorios.MapGet("/evolucao-despesa", async (
            int anoDe, int mesDe, int anoAte, int mesAte, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterEvolucaoDespesaQuery(anoDe, mesDe, anoAte, mesAte), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        // Mapa de cargos do quadro: vagas autorizadas x ocupadas x vagas, com totais por tipo e geral.
        relatorios.MapGet("/mapa-cargos", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterMapaCargosQuery(), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");

        // Demonstrativo de despesa de pessoal para o TCE numa competencia (totais por fonte e por UO +
        // contribuicao previdenciaria do segurado). A remessa formatada SIAPC/PAD e do modulo Transparencia.
        relatorios.MapGet("/demonstrativo-tce", async (
            int ano, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterDemonstrativoTceQuery(ano, mes), cancellationToken)))
            .RequirePermission("recursoshumanos.ver");
    }
}
