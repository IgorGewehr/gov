using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Pasep;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Portarias;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Portarias;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;

/// <summary>
/// Endpoints de ATOS DE PESSOAL (PARIDADE-PoC Trilha A): portarias (emissao/revogacao/consulta com
/// numeracao sequencial por exercicio), reajuste salarial em lote e apuracao do PASEP.
/// </summary>
internal static partial class RecursosHumanosEndpoints
{
    private static void MapearPessoal(RouteGroupBuilder grupo)
    {
        MapearPortarias(grupo);
        MapearReajuste(grupo);
        MapearPasep(grupo);
    }

    private static void MapearPortarias(RouteGroupBuilder grupo)
    {
        // PORTARIAS / ATOS DE PESSOAL (SW-A7): emissao com numeracao SEQUENCIAL por exercicio/tenant
        // (o cliente NAO informa o numero), vinculo opcional ao servidor, texto + situacao. Uso diario.
        var portarias = grupo.MapGroup("/portarias").WithTags("RecursosHumanos.Portarias");

        portarias.MapPost("/", async (
            EmitirPortariaCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        portarias.MapGet("/", async (
            TipoPortaria? tipo, SituacaoPortaria? situacao, int? exercicio, Guid? servidorId,
            int? pagina, int? tamanho, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(
                new BuscarPortariasQuery(tipo, situacao, exercicio, servidorId, pagina, tamanho), ct)))
            .RequirePermission("recursoshumanos.ver");

        portarias.MapGet("/{portariaId:guid}", async (
            Guid portariaId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ObterPortariaQuery(portariaId), ct)))
            .RequirePermission("recursoshumanos.ver");

        portarias.MapPost("/{portariaId:guid}/revogacao", async (
            Guid portariaId, RevogarPortariaPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RevogarPortariaCommand(portariaId, payload.Motivo), ct);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private static void MapearReajuste(RouteGroupBuilder grupo)
    {
        // REAJUSTE SALARIAL EM LOTE (SW-A8): aplica um percentual linear ao vencimento dos cargos ATIVOS
        // (revisao geral anual — CF art. 37, X), opcionalmente restrito a um tipo. Auditado (VencimentoAlterado).
        grupo.MapPost("/cargos/reajuste-lote", async (
            AplicarReajusteEmLotePayload payload, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new AplicarReajusteEmLoteCommand(payload.Percentual, payload.Tipo), ct)))
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private static void MapearPasep(RouteGroupBuilder grupo)
    {
        // PASEP (SW-A8): apuracao da base (folha bruta da competencia) x aliquota parametrizavel (1% padrao).
        // Transmissao/recolhimento real = M10 (// TODO(M10)).
        var pasep = grupo.MapGroup("/pasep").WithTags("RecursosHumanos.Pasep");

        pasep.MapPost("/apuracoes", async (
            ApurarPasepPayload payload, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(new ApurarPasepCommand(payload.Ano, payload.Mes, payload.AliquotaOverride), ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        pasep.MapGet("/apuracoes", async (
            int ano, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListarApuracoesPasepQuery(ano), ct)))
            .RequirePermission("recursoshumanos.ver");
    }

    private sealed record RevogarPortariaPayload(string Motivo);

    private sealed record AplicarReajusteEmLotePayload(decimal Percentual, TipoCargo? Tipo);

    private sealed record ApurarPasepPayload(int Ano, int Mes, decimal? AliquotaOverride);
}
