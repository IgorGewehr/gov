using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.ProcessosTrabalhistas;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ProcessosTrabalhistas;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;

/// <summary>
/// Endpoints de PROCESSOS TRABALHISTAS: cadastro/acompanhamento (numero CNJ, vara, reclamante, objeto,
/// valores, situacao) e controle da provisao contabil (NBC TG 25): reavaliacao de prognostico, acordo,
/// condenacao, improcedencia, arquivamento e o demonstrativo de provisoes vigentes.
/// </summary>
internal static partial class RecursosHumanosEndpoints
{
    private static void MapearProcessosTrabalhistas(RouteGroupBuilder grupo)
    {
        var processos = grupo.MapGroup("/processos-trabalhistas").WithTags("RecursosHumanos.ProcessosTrabalhistas");

        // Cadastra um processo trabalhista contra o ente (define a provisao a partir do prognostico).
        processos.MapPost("/", async (
            CadastrarProcessoTrabalhistaCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Busca paginada por situacao/prognostico/termo (numero/reclamante).
        processos.MapGet("/", async (
            SituacaoProcessoTrabalhista? situacao, PrognosticoPerda? prognostico, string? termo,
            int? pagina, int? tamanho, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(
                new BuscarProcessosTrabalhistasQuery(situacao, prognostico, termo, pagina, tamanho), ct)))
            .RequirePermission("recursoshumanos.ver");

        // Demonstrativo de provisoes trabalhistas vigentes (passivo NBC TG 25).
        processos.MapGet("/demonstrativo-provisao", async (ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ObterDemonstrativoProvisaoQuery(), ct)))
            .RequirePermission("recursoshumanos.ver");

        // Detalhe completo do processo.
        processos.MapGet("/{processoId:guid}", async (Guid processoId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ObterProcessoTrabalhistaQuery(processoId), ct)))
            .RequirePermission("recursoshumanos.ver");

        // Reavalia o prognostico de perda (recalcula a provisao).
        processos.MapPost("/{processoId:guid}/prognostico", async (
            Guid processoId, PrognosticoPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ReavaliarPrognosticoCommand(processoId, payload.Prognostico), ct);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        // Homologa um acordo (encerra com valor de acordo).
        processos.MapPost("/{processoId:guid}/acordo", async (
            Guid processoId, AcordoPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RegistrarAcordoCommand(processoId, payload.Valor, payload.Data), ct);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        // Registra condenacao transitada em julgado (encerra com valor de condenacao).
        processos.MapPost("/{processoId:guid}/condenacao", async (
            Guid processoId, CondenacaoPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RegistrarCondenacaoCommand(processoId, payload.Valor, payload.Data), ct);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        // Encerra por improcedencia/extincao sem condenacao (zera a provisao).
        processos.MapPost("/{processoId:guid}/improcedencia", async (
            Guid processoId, ImprocedenciaPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RegistrarImprocedenciaCommand(processoId, payload.Data), ct);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");

        // Arquiva definitivamente um processo ja encerrado.
        processos.MapPost("/{processoId:guid}/arquivamento", async (
            Guid processoId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ArquivarProcessoTrabalhistaCommand(processoId), ct);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private sealed record PrognosticoPayload(PrognosticoPerda Prognostico);

    private sealed record AcordoPayload(decimal Valor, DateOnly Data);

    private sealed record CondenacaoPayload(decimal Valor, DateOnly Data);

    private sealed record ImprocedenciaPayload(DateOnly Data);
}
