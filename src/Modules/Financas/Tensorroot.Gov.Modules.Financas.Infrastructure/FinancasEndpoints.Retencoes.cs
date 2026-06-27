using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Financas.Application.Retencoes;
using Tensorroot.Gov.Modules.Financas.Domain.Recolhimentos;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) das retenções/consignações e do recolhimento extra-orçamentário
/// (IN RFB 1.234/2012 — IRRF de serviços; Lei 4.320/64 — dispêndios extra-orçamentários).
/// Leituras exigem "financas.ver"; mutações exigem "financas.gerenciar".
/// </summary>
internal static partial class FinancasEndpoints
{
    private static void MapearRetencoes(RouteGroupBuilder grupo)
    {
        // Semeia a tabela de IRRF/PJ vigente (IN RFB 1234/2012) para o tenant.
        grupo.MapPost("/retencoes/tabela-irrf/semear", async (ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { faixas = await sender.Send(new SemearTabelaIrrfServicosCommand(), cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        // Consulta a tabela de IRRF/PJ vigente em uma data (default = hoje).
        grupo.MapGet("/retencoes/tabela-irrf", async (DateOnly? data, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ConsultarTabelaIrrfQuery(data ?? DateOnly.FromDateTime(DateTime.UtcNow)), cancellationToken) is { } tabela
                ? Results.Ok(tabela)
                : Results.NotFound())
            .RequirePermission("financas.ver");

        // Adiciona uma retenção/consignação a uma liquidação (IRRF auto pela tabela; demais por valor).
        grupo.MapPost("/liquidacoes/{liquidacaoId:guid}/retencoes", async (
            Guid liquidacaoId, AdicionarRetencaoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                id = await sender.Send(
                    new AdicionarRetencaoCommand(
                        liquidacaoId,
                        payload.Natureza,
                        payload.EnquadramentoIrrf,
                        payload.BaseCalculo,
                        payload.ValorInformado,
                        payload.AliquotaInformada,
                        payload.CodigoReceita,
                        payload.FavorecidoDocumento,
                        payload.Descricao),
                    cancellationToken),
            }))
            .RequirePermission("financas.gerenciar");

        // Emite uma guia de recolhimento reunindo retenções pendentes de mesma natureza.
        grupo.MapPost("/retencoes/guias", async (EmitirGuiaRecolhimentoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        // Registra o recolhimento efetivo de uma guia (baixa do passivo extra-orçamentário).
        grupo.MapPost("/retencoes/guias/{guiaId:guid}/recolher", async (
            Guid guiaId, RecolherGuiaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RecolherGuiaCommand(guiaId, payload.DataRecolhimento), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        // Cancela uma guia ainda não recolhida (reabre as retenções vinculadas).
        grupo.MapPost("/retencoes/guias/{guiaId:guid}/cancelar", async (
            Guid guiaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarGuiaCommand(guiaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        // Lista guias de recolhimento (filtro opcional por situação).
        grupo.MapGet("/retencoes/guias", async (SituacaoGuiaRecolhimento? situacao, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarGuiasRecolhimentoQuery(situacao), cancellationToken)))
            .RequirePermission("financas.ver");
    }

    private sealed record AdicionarRetencaoPayload(
        Domain.Retencoes.NaturezaRetencao Natureza,
        string? EnquadramentoIrrf,
        decimal? BaseCalculo,
        decimal? ValorInformado,
        decimal? AliquotaInformada,
        string? CodigoReceita,
        string? FavorecidoDocumento,
        string? Descricao);

    private sealed record RecolherGuiaPayload(DateOnly DataRecolhimento);

    private sealed record GerarCnabPayload(
        string CodigoBanco,
        int TipoInscricao,
        string NumeroInscricao,
        string Convenio,
        string? DvAgencia,
        string? DvConta,
        string? DvAgenciaConta,
        string NomeEmpresa,
        int FormaLancamento,
        int SequencialArquivo);
}
