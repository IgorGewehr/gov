using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.PlanoCarreira;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;

/// <summary>
/// Endpoints do PLANO DE CARGOS E SALARIOS (PCCS): instituicao do plano com matriz salarial, consulta da
/// grade derivada, enquadramento do servidor e movimentacoes (progressao horizontal / promocao vertical),
/// com efeito remuneratorio no cargo (reuso de Cargo.AlterarVencimento, auditado).
/// </summary>
internal static partial class RecursosHumanosEndpoints
{
    private static void MapearPlanoCarreira(RouteGroupBuilder grupo)
    {
        var planos = grupo.MapGroup("/planos-carreira").WithTags("RecursosHumanos.PlanoCarreira");

        // Institui um plano de carreira (matriz classe x referencia + regras de movimentacao).
        planos.MapPost("/", async (
            InstituirPlanoCarreiraCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Lista os planos de carreira do tenant.
        planos.MapGet("/", async (ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListarPlanosCarreiraQuery(), ct)))
            .RequirePermission("recursoshumanos.ver");

        // Detalhe do plano com a matriz salarial completa (vencimento derivado de cada celula).
        planos.MapGet("/{planoId:guid}", async (Guid planoId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ObterPlanoCarreiraQuery(planoId), ct)))
            .RequirePermission("recursoshumanos.ver");

        // ENQUADRAMENTO inicial do servidor numa posicao da matriz (aplica o vencimento ao cargo).
        planos.MapPost("/enquadramentos", async (
            EnquadrarServidorCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Enquadramento vigente de um servidor (posicao + historico de movimentacoes).
        planos.MapGet("/servidores/{servidorId:guid}/enquadramento", async (
            Guid servidorId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ObterEnquadramentoDoServidorQuery(servidorId), ct)))
            .RequirePermission("recursoshumanos.ver");

        // PROGRESSAO HORIZONTAL (avanco de referencia, por tempo/avaliacao) — retorna o novo vencimento.
        planos.MapPost("/servidores/{servidorId:guid}/progressao", async (
            Guid servidorId, ProgressaoPayload payload, ISender sender, CancellationToken ct)
            => Results.Ok(new
            {
                vencimento = await sender.Send(
                    new ConcederProgressaoCommand(servidorId, payload.Criterio, payload.Fundamento, payload.PortariaId, payload.DataEfeito), ct),
            }))
            .RequirePermission("recursoshumanos.gerenciar");

        // PROMOCAO VERTICAL (avanco de classe, por titulacao/antiguidade) — retorna o novo vencimento.
        planos.MapPost("/servidores/{servidorId:guid}/promocao", async (
            Guid servidorId, PromocaoPayload payload, ISender sender, CancellationToken ct)
            => Results.Ok(new
            {
                vencimento = await sender.Send(
                    new ConcederPromocaoCommand(servidorId, payload.Fundamento, payload.PortariaId, payload.DataEfeito), ct),
            }))
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private sealed record ProgressaoPayload(
        CriterioProgressao Criterio,
        string Fundamento,
        Guid? PortariaId,
        DateOnly? DataEfeito);

    private sealed record PromocaoPayload(
        string Fundamento,
        Guid? PortariaId,
        DateOnly? DataEfeito);
}
