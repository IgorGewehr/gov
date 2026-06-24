using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Patrimonio.Application.Obras;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) das Obras (W9.3 — Lei 14.133/2021): obra como bem patrimonial em
/// formacao. Abertura a partir do contrato (vinculo por ContratoId), cronograma fisico-financeiro
/// (curva S), RDO + medicao (medicao aprovada libera liquidacao em Financas), fiscalizacao (fiscal
/// designado, ocorrencias, paralisacao/reinicio), conclusao -> incorporacao patrimonial. Prazos
/// art. 94 §3 via calendario transversal (W9.1). SICOE = M10. Extraido como partial para manter o
/// arquivo principal dentro do limite de manutenibilidade.
/// </summary>
internal static partial class PatrimonioEndpoints
{
    private static void MapearObras(RouteGroupBuilder grupo)
    {
        // Abrir obra a partir de contrato NLLC (Planejada).
        grupo.MapPost("/obras", async (
            AbrirObraCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("patrimonio.gerenciar");

        // NAVEGABILIDADE (Onda 0): lista/busca paginada por objeto/municipio, filtro situacao.
        grupo.MapGet("/obras", async (
            string? termo, SituacaoObra? situacao, int? pagina, int? tamanho,
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarObrasQuery(termo, situacao, pagina, tamanho), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        // Ficha da obra (cronograma, medicoes, RDOs, fiscalizacao/ocorrencias).
        grupo.MapGet("/obras/{obraId:guid}", async (
            Guid obraId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterObraQuery(obraId), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        // Cronograma fisico-financeiro (curva S) — I-3.
        grupo.MapPost("/obras/{obraId:guid}/cronograma", async (
            Guid obraId, DefinirCronogramaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DefinirCronogramaCommand(obraId, payload.Etapas), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        // Ordem de inicio de servico (Planejada -> EmExecucao).
        grupo.MapPost("/obras/{obraId:guid}/ordem-inicio", async (
            Guid obraId, OrdemInicioPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EmitirOrdemInicioCommand(obraId, payload.Data), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        // Fiscalizacao: designar fiscal (art. 117 / I-10).
        grupo.MapPost("/obras/{obraId:guid}/fiscal", async (
            Guid obraId, DesignarFiscalPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DesignarFiscalCommand(obraId, payload.FiscalId, payload.Desde, payload.AtoDesignacao), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        // RDO (fiscalizacao continua — I-8/I-9).
        grupo.MapPost("/obras/{obraId:guid}/rdos", async (
            Guid obraId, RdoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(new RegistrarRdoCommand(
                obraId, payload.Data, payload.CondicaoTempo, payload.EfetivoMaoDeObra,
                payload.EquipamentosMobilizados, payload.AtividadesExecutadas, payload.ResponsavelTecnicoId, payload.Ocorrencias), cancellationToken) }))
            .RequirePermission("patrimonio.gerenciar");

        // Medicao: registrar boletim (rascunho).
        grupo.MapPost("/obras/{obraId:guid}/medicoes", async (
            Guid obraId, MedicaoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(new RegistrarMedicaoCommand(
                obraId, payload.CompetenciaAno, payload.CompetenciaMes, payload.PeriodoInicio, payload.PeriodoFim, payload.Avancos), cancellationToken) }))
            .RequirePermission("patrimonio.gerenciar");

        // Medicao: aprovar (libera liquidacao em Financas — Lei 4.320 art. 63 / I-1/I-8/I-10).
        // SEGREGACAO DE FUNCAO (art. 117): o aprovador NAO trafega no corpo — e derivado do subject
        // do JWT no handler; o agregado so aprova se o usuario autenticado FOR o fiscal designado.
        grupo.MapPost("/obras/{obraId:guid}/medicoes/{medicaoId:guid}/aprovacao", async (
            Guid obraId, Guid medicaoId, AprovarMedicaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AprovarMedicaoCommand(obraId, medicaoId, payload.DataAprovacao), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        // Medicao: rejeitar.
        grupo.MapPost("/obras/{obraId:guid}/medicoes/{medicaoId:guid}/rejeicao", async (
            Guid obraId, Guid medicaoId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RejeitarMedicaoCommand(obraId, medicaoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        // Fiscalizacao: ocorrencia.
        grupo.MapPost("/obras/{obraId:guid}/ocorrencias", async (
            Guid obraId, OcorrenciaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(new RegistrarOcorrenciaCommand(
                obraId, payload.Data, payload.Tipo, payload.Descricao, payload.RegistradaPorId), cancellationToken) }))
            .RequirePermission("patrimonio.gerenciar");

        // Paralisacao / reinicio (I-15).
        grupo.MapPost("/obras/{obraId:guid}/paralisacao", async (
            Guid obraId, ParalisacaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ParalisarObraCommand(obraId, payload.Motivo, payload.Data), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/obras/{obraId:guid}/reinicio", async (
            Guid obraId, DataPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ReiniciarObraCommand(obraId, payload.Data), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        // Conclusao -> incorporacao patrimonial (I-12/I-13); retorna o id do bem incorporado.
        grupo.MapPost("/obras/{obraId:guid}/conclusao", async (
            Guid obraId, ConclusaoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { bemPatrimonialId = await sender.Send(new ConcluirObraCommand(
                obraId, payload.DataConclusao, payload.VidaUtilMeses, payload.TipoBem, payload.ValorResidual, payload.ValorTerreno), cancellationToken) }))
            .RequirePermission("patrimonio.gerenciar");

        // Rescisao (terminal antes da conclusao).
        grupo.MapPost("/obras/{obraId:guid}/rescisao", async (
            Guid obraId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RescindirObraCommand(obraId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");
    }

    private sealed record DefinirCronogramaPayload(IReadOnlyList<EtapaCronogramaInput> Etapas);

    private sealed record OrdemInicioPayload(DateOnly Data);

    private sealed record DesignarFiscalPayload(Guid FiscalId, DateOnly Desde, string AtoDesignacao);

    private sealed record RdoPayload(
        DateOnly Data,
        string CondicaoTempo,
        int EfetivoMaoDeObra,
        string EquipamentosMobilizados,
        string AtividadesExecutadas,
        Guid ResponsavelTecnicoId,
        string? Ocorrencias);

    private sealed record MedicaoPayload(
        int CompetenciaAno,
        int CompetenciaMes,
        DateOnly PeriodoInicio,
        DateOnly PeriodoFim,
        IReadOnlyList<AvancoEtapaInput> Avancos);

    // Sem FiscalId: o aprovador e SEMPRE o subject do JWT (segregacao de funcao — art. 117 / I-10),
    // nunca um id enviado pelo cliente (evita impersonacao do fiscal designado).
    private sealed record AprovarMedicaoPayload(DateOnly DataAprovacao);

    private sealed record OcorrenciaPayload(DateOnly Data, int Tipo, string Descricao, Guid RegistradaPorId);

    private sealed record ParalisacaoPayload(int Motivo, DateOnly Data);

    private sealed record ConclusaoPayload(
        DateOnly DataConclusao,
        int VidaUtilMeses,
        int TipoBem,
        decimal ValorResidual,
        decimal ValorTerreno);

    private sealed record DataPayload(DateOnly Data);

    private sealed record MotivoPayload(string Motivo);
}
