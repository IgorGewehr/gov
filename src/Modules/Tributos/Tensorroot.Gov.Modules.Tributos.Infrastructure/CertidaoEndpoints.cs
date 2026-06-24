using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Tributos.Application.Certidoes;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) da Certidão de regularidade fiscal — CND/CPEN (CTN arts. 205/206 —
/// PARIDADE-PoC SW-A3). Emissão administrativa (RBAC tributos.*) e conferência de autenticidade
/// (anônima — serviço público de validação de documento, sem vazamento de dado de terceiro).
/// </summary>
internal static class CertidaoEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/tributos/certidoes").WithTags("Tributos.Certidoes");

        // EMITE a certidão de regularidade do contribuinte: apura a situação fiscal e DECIDE o tipo
        // (Negativa / Positiva-com-efeito-Negativa / Positiva). Verbo de leitura (não muta tributo).
        grupo.MapPost("/regularidade", async (
            EmitirCertidaoRegularidadeCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(comando, cancellationToken)))
            .RequirePermission("tributos.ver");

        // CONFERE a autenticidade de uma certidão apresentada por terceiro (número + código). PÚBLICO:
        // serviço de validação de documento; retorna apenas se é autêntica/vigente, sem dado sensível
        // adicional quando não confere (anti-enumeração).
        grupo.MapGet("/conferir", async (
            string numero, string codigo, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ConferirCertidaoRegularidadeQuery(numero, codigo), cancellationToken)))
            .AllowAnonymous();
    }
}
