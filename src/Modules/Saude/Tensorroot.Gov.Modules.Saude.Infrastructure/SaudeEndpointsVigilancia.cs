using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Saude.Application.Vigilancia;
using Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure;

/// <summary>
/// Endpoints HTTP da Vigilancia Sanitaria (Onda 3c-2): estabelecimentos fiscalizaveis, inspecoes/vistorias,
/// autos (infracao/intimacao) e licencas/alvaras sanitarios. Particionado do <see cref="SaudeEndpoints"/>
/// principal para manutenibilidade (god-file &lt; 500 linhas). Operacao 100% local —
/// // TODO(M10): integracao SINAVISA/e-SUS VS atras de ACL (apos credencial estadual/DATASUS).
/// </summary>
internal static partial class SaudeEndpoints
{
    private static void MapearVigilancia(RouteGroupBuilder grupo)
    {
        var visa = grupo.MapGroup("/vigilancia");

        // ---------- Estabelecimentos sujeitos a VISA ----------
        visa.MapPost("/estabelecimentos", async (
            CadastrarEstabelecimentoFiscalizavelCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("saude.vigilancia.gerenciar");

        visa.MapGet("/estabelecimentos", async (
            string? termo, RamoVisa? ramo, GrauRiscoSanitario? risco, SituacaoEstabelecimentoVisa? situacao,
            int? pagina, int? tamanho, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new BuscarEstabelecimentosFiscalizaveisQuery(termo, ramo, risco, situacao, pagina, tamanho), cancellationToken)))
            .RequirePermission("saude.vigilancia.ver");

        visa.MapPut("/estabelecimentos/{id:guid}/classificacao", async (
            Guid id, ReclassificarPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ReclassificarEstabelecimentoCommand(id, payload.Ramo, payload.Risco), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.vigilancia.gerenciar");

        visa.MapPost("/estabelecimentos/{id:guid}/interdicao", async (
            Guid id, MotivoVisaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new InterditarEstabelecimentoCommand(id, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.vigilancia.gerenciar");

        visa.MapPost("/estabelecimentos/{id:guid}/levantamento-interdicao", async (
            Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new LevantarInterdicaoCommand(id), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.vigilancia.gerenciar");

        // ---------- Inspecoes/vistorias ----------
        visa.MapPost("/inspecoes", async (
            AbrirInspecaoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("saude.vigilancia.inspecionar");

        visa.MapGet("/inspecoes/agenda", async (
            DateOnly de, DateOnly ate, SituacaoInspecao? situacao, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarAgendaInspecoesQuery(de, ate, situacao), cancellationToken)))
            .RequirePermission("saude.vigilancia.ver");

        visa.MapGet("/inspecoes/{id:guid}", async (
            Guid id, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterInspecaoPorIdQuery(id), cancellationToken)))
            .RequirePermission("saude.vigilancia.ver");

        visa.MapPost("/inspecoes/{id:guid}/itens", async (
            Guid id, ItemInspecaoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(
                new RegistrarItemInspecaoCommand(id, payload.Requisito, payload.Conformidade, payload.Observacao), cancellationToken) }))
            .RequirePermission("saude.vigilancia.inspecionar");

        visa.MapPost("/inspecoes/{id:guid}/conclusao", async (
            Guid id, ConcluirInspecaoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { resultado = await sender.Send(
                new ConcluirInspecaoCommand(id, payload.HouveInfracaoGrave), cancellationToken) }))
            .RequirePermission("saude.vigilancia.inspecionar");

        visa.MapPost("/inspecoes/{id:guid}/cancelamento", async (
            Guid id, MotivoVisaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarInspecaoCommand(id, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.vigilancia.inspecionar");

        // ---------- Autos (infracao/intimacao) — pendurados na inspecao ----------
        visa.MapPost("/inspecoes/{inspecaoId:guid}/autos", async (
            Guid inspecaoId, LavrarAutoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(
                new LavrarAutoCommand(payload.EstabelecimentoId, inspecaoId, payload.Tipo, payload.Numero, payload.Fundamentacao, payload.PrazoFinal, payload.ValorMulta), cancellationToken) }))
            .RequirePermission("saude.vigilancia.autuar");

        visa.MapGet("/autos", async (
            SituacaoAutoVisa? status, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarAutosQuery(status), cancellationToken)))
            .RequirePermission("saude.vigilancia.ver");

        visa.MapPost("/autos/{id:guid}/defesa", async (
            Guid id, DefesaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ApresentarDefesaAutoCommand(id, payload.Texto), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.vigilancia.autuar");

        visa.MapPost("/autos/{id:guid}/julgamento", async (
            Guid id, JulgamentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new JulgarAutoCommand(id, payload.Deferir), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.vigilancia.autuar");

        visa.MapPost("/autos/{id:guid}/regularizacao", async (
            Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegularizarIntimacaoCommand(id), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.vigilancia.autuar");

        // ---------- Licencas/alvaras sanitarios ----------
        visa.MapPost("/licencas", async (
            EmitirLicencaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("saude.vigilancia.licenciar");

        visa.MapGet("/estabelecimentos/{estabelecimentoId:guid}/licencas", async (
            Guid estabelecimentoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarLicencasDoEstabelecimentoQuery(estabelecimentoId), cancellationToken)))
            .RequirePermission("saude.vigilancia.ver");

        visa.MapGet("/licencas/a-vencer", async (
            int? dias, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarLicencasAVencerQuery(dias), cancellationToken)))
            .RequirePermission("saude.vigilancia.ver");

        visa.MapPost("/licencas/{id:guid}/renovacao", async (
            Guid id, RenovarLicencaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(
                new RenovarLicencaCommand(id, payload.Numero, payload.ValidadeAte, payload.InspecaoId), cancellationToken) }))
            .RequirePermission("saude.vigilancia.licenciar");

        visa.MapPost("/licencas/{id:guid}/cassacao", async (
            Guid id, MotivoVisaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CassarLicencaCommand(id, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.vigilancia.licenciar");
    }

    private sealed record ReclassificarPayload(RamoVisa Ramo, GrauRiscoSanitario Risco);

    private sealed record MotivoVisaPayload(string Motivo);

    private sealed record ItemInspecaoPayload(string Requisito, ConformidadeItem Conformidade, string? Observacao);

    private sealed record ConcluirInspecaoPayload(bool HouveInfracaoGrave);

    private sealed record LavrarAutoPayload(
        Guid EstabelecimentoId,
        TipoAutoVisa Tipo,
        string Numero,
        string Fundamentacao,
        DateOnly PrazoFinal,
        decimal? ValorMulta);

    private sealed record DefesaPayload(string Texto);

    private sealed record JulgamentoPayload(bool Deferir);

    private sealed record RenovarLicencaPayload(string Numero, DateOnly ValidadeAte, Guid? InspecaoId);
}
