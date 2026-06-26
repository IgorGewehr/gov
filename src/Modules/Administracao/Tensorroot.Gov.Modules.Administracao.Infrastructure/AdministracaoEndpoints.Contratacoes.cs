using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.Modules.Administracao.Application.Contratos;
using Tensorroot.Gov.Modules.Administracao.Application.Dispensas;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure;

/// <summary>
/// Particao dos endpoints de Administracao referente a CONTRATACAO DIRETA (dispensa eletronica — Lei
/// 14.133/2021, art. 75) e CONTRATOS (celebracao, execucao, PNCP). Separada de <c>AdministracaoEndpoints.cs</c>
/// para manter cada arquivo abaixo do limite de manutenibilidade (god-file &lt; 500 linhas).
/// </summary>
internal static partial class AdministracaoEndpoints
{
    private static void MapDispensas(RouteGroupBuilder grupo)
    {
        // Abertura (rascunho) — Lei 14.133/2021, art. 75, I/II; IN SEGES/ME 67/2021.
        grupo.MapPost("/dispensas", async (
            AbrirDispensaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("administracao.gerenciar");

        // Inclusao de item (com fail-closed do teto legal vigente no agregado).
        grupo.MapPost("/dispensas/{dispensaId:guid}/itens", async (
            Guid dispensaId, AdicionarItemDispensaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                itemId = await sender.Send(
                    new AdicionarItemDispensaCommand(dispensaId, payload.ItemCatalogoId, payload.Descricao, payload.Quantidade, payload.ValorUnitarioEstimado),
                    cancellationToken),
            }))
            .RequirePermission("administracao.gerenciar");

        // Publicacao do aviso de contratacao direta (valida o prazo minimo de divulgacao).
        grupo.MapPost("/dispensas/{dispensaId:guid}/aviso", async (
            Guid dispensaId, PublicarAvisoDispensaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new PublicarAvisoDispensaCommand(dispensaId, payload.NumeroAviso, payload.AberturaDisputa), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        // Abertura da etapa de lances.
        grupo.MapPost("/dispensas/{dispensaId:guid}/disputa/abrir", async (
            Guid dispensaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AbrirDisputaDispensaCommand(dispensaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        // Registro de lance (cotacao) — fail-closed para fornecedor impedido; lance sucessivo deve melhorar.
        grupo.MapPost("/dispensas/{dispensaId:guid}/lances", async (
            Guid dispensaId, RegistrarLanceDispensaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                cotacaoId = await sender.Send(
                    new RegistrarLanceDispensaCommand(dispensaId, payload.ItemId, payload.FornecedorId, payload.Valor),
                    cancellationToken),
            }))
            .RequirePermission("administracao.gerenciar");

        // Encerramento da disputa + julgamento (indica vencedora pelo criterio).
        grupo.MapPost("/dispensas/{dispensaId:guid}/disputa/encerrar", async (
            Guid dispensaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EncerrarDisputaDispensaCommand(dispensaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        // Homologacao (autoridade competente) — exige vencedor habilitado e nao impedido.
        grupo.MapPost("/dispensas/{dispensaId:guid}/homologar", async (
            Guid dispensaId, HomologarDispensaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new HomologarDispensaCommand(dispensaId, payload.VencedorHabilitado), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        // Atos terminais.
        grupo.MapPost("/dispensas/{dispensaId:guid}/fracassar", async (
            Guid dispensaId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DeclararDispensaFracassadaCommand(dispensaId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/dispensas/{dispensaId:guid}/deserta", async (
            Guid dispensaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DeclararDispensaDesertaCommand(dispensaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/dispensas/{dispensaId:guid}/revogar", async (
            Guid dispensaId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RevogarDispensaCommand(dispensaId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/dispensas/{dispensaId:guid}/anular", async (
            Guid dispensaId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AnularDispensaCommand(dispensaId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapGet("/dispensas/{dispensaId:guid}", async (
            Guid dispensaId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterDispensaPorIdQuery(dispensaId), cancellationToken) is { } detalhe
                ? Results.Ok(detalhe)
                : Results.NotFound())
            .RequirePermission("administracao.ver");

        grupo.MapGet("/dispensas", async (
            SituacaoDispensa situacao, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarDispensasPorSituacaoQuery(situacao), cancellationToken)))
            .RequirePermission("administracao.ver");
    }

    private static void MapContratos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/contratos", async (
            CelebrarContratoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPost("/contratos/{contratoId:guid}/contrato-pncp", async (
            Guid contratoId, PublicarContratoPncpPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            // W9.1/L2: o numero de controle NAO vem do cliente — a ACL (IPncpGateway) transmite e devolve.
            // Os campos do schema "Inserir Contrato/Empenho" 2.3.5 que nao vivem no agregado vem do payload.
            await sender.Send(
                new PublicarContratoNoPncpCommand(
                    contratoId,
                    payload.CnpjOrgao,
                    payload.CodigoUnidade,
                    payload.NumeroContratoInterno,
                    payload.AnoContrato,
                    payload.Processo,
                    payload.NiFornecedor,
                    payload.TipoPessoaFornecedor,
                    payload.NomeRazaoSocialFornecedor,
                    payload.TipoContratoId,
                    payload.CategoriaProcessoId,
                    payload.NumeroParcelas,
                    payload.CnpjCompra,
                    payload.AnoCompra,
                    payload.SequencialCompra,
                    payload.NumeroControlePncpCompra,
                    payload.FrutoAdesao),
                cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        // W9.1.d: varredura dos prazos de divulgacao no PNCP (art. 94) -> alertas ao Portal do Gestor.
        // Acionada por scheduler/worker (idempotente; alertas viajam pelo Outbox).
        grupo.MapPost("/contratos/prazos-pncp/varrer", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { alertas = await sender.Send(new VarrerPrazosPncpCommand(), cancellationToken) }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPost("/contratos/{contratoId:guid}/iniciar-execucao", async (
            Guid contratoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new IniciarExecucaoContratoCommand(contratoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/contratos/{contratoId:guid}/encerrar", async (
            Guid contratoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EncerrarContratoCommand(contratoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapGet("/contratos/{contratoId:guid}", async (
            Guid contratoId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterContratoPorIdQuery(contratoId), cancellationToken) is { } detalhe
                ? Results.Ok(detalhe)
                : Results.NotFound())
            .RequirePermission("administracao.ver");

        grupo.MapGet("/contratos/vigentes", async (
            DateOnly referencia, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarContratosVigentesQuery(referencia), cancellationToken)))
            .RequirePermission("administracao.ver");

        grupo.MapGet("/fornecedores/{fornecedorId:guid}/contratos", async (
            Guid fornecedorId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarContratosPorFornecedorQuery(fornecedorId), cancellationToken)))
            .RequirePermission("administracao.ver");
    }
}
