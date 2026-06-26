using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Tributos.Application.Desif;
using Tensorroot.Gov.Modules.Tributos.Application.Domicilio;
using Tensorroot.Gov.Modules.Tributos.Application.Sim;
using Tensorroot.Gov.Modules.Tributos.Domain.Domicilio;
using Tensorroot.Gov.Modules.Tributos.Domain.Sim;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) dos gaps de PARIDADE com o incumbente SAPI: DES-IF (Declaração
/// Eletrônica de Serviços de Instituições Financeiras), Título de Registro do S.I.M. (Serviço de
/// Inspeção Municipal) e Domicílio Eletrônico do Contribuinte (DEC). RBAC por permissões tributos.*
/// (negar por padrão — CLAUDE.md §6).
/// </summary>
internal static class ParidadeSapiEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        MapDesif(endpoints);
        MapSim(endpoints);
        MapDomicilio(endpoints);
    }

    // DES-IF — Apuração Mensal do ISSQN (Módulo 2 ABRASF): declaração do banco por subtítulo COSIF +
    // deduções (Registro 0440); constitui o crédito do ISSQN a recolher (CTN art. 150).
    private static void MapDesif(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/tributos/desif").WithTags("Tributos.DesIf");

        grupo.MapPost("/contribuintes/{contribuinteId:guid}/apuracao-mensal", async (
            Guid contribuinteId, EntregarDesifPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new EntregarDesifCommand(
                    contribuinteId,
                    payload.Ano,
                    payload.Mes,
                    payload.FundamentoLegal,
                    payload.DeducoesReceita,
                    payload.IncentivosFiscais,
                    payload.DepositosJudiciais,
                    payload.VencimentoIssqn,
                    payload.Subtitulos),
                cancellationToken)))
            .RequirePermission("tributos.gerenciar");
    }

    // S.I.M. — Título de registro de estabelecimento (produtos de origem animal/vegetal): requerer →
    // habilitar produtos → conceder (número do S.I.M.) → suspender/reativar/cassar.
    private static void MapSim(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/tributos/sim").WithTags("Tributos.Sim");

        grupo.MapPost("/titulos", async (
            RequererTituloSimCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        grupo.MapPost("/titulos/{tituloId:guid}/conceder", async (
            Guid tituloId, ConcederTituloSimPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new ConcederTituloSimCommand(tituloId, payload.DataRegistro, payload.FimVigencia), cancellationToken)))
            .RequirePermission("tributos.gerenciar");

        grupo.MapPost("/titulos/{tituloId:guid}/produtos", async (
            Guid tituloId, ProdutoSimInput produto, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await EnviarOk(sender, new HabilitarProdutoSimCommand(tituloId, produto), cancellationToken)))
            .RequirePermission("tributos.gerenciar");

        grupo.MapPost("/titulos/{tituloId:guid}/situacao", async (
            Guid tituloId, AlterarSituacaoSimPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await EnviarOk(sender, new AlterarSituacaoTituloSimCommand(tituloId, payload.Acao, payload.Motivo), cancellationToken)))
            .RequirePermission("tributos.gerenciar");
    }

    // DEC — Domicílio Eletrônico do Contribuinte: aderir → disponibilizar mensagem fiscal → cancelar.
    // A ciência (consulta) e a leitura da caixa do PRÓPRIO cidadão são expostas via Contracts (Portal).
    private static void MapDomicilio(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/tributos/domicilio-eletronico").WithTags("Tributos.DomicilioEletronico");

        grupo.MapPost("/contribuintes/{contribuinteId:guid}/aderir", async (
            Guid contribuinteId, AderirDomicilioPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(
                new AderirDomicilioEletronicoCommand(contribuinteId, payload.DataAdesao, payload.DiasCienciaTacita), cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        grupo.MapPost("/contribuintes/{contribuinteId:guid}/mensagens", async (
            Guid contribuinteId, DisponibilizarMensagemPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new DisponibilizarMensagemFiscalCommand(
                    contribuinteId,
                    payload.Tipo,
                    payload.Assunto,
                    payload.Corpo,
                    payload.DataDisponibilizacao,
                    payload.DiasPrazoManifestacao,
                    payload.ReferenciaExterna),
                cancellationToken)))
            .RequirePermission("tributos.gerenciar");

        grupo.MapPost("/contribuintes/{contribuinteId:guid}/cancelar", async (
            Guid contribuinteId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await EnviarOk(sender, new CancelarDomicilioEletronicoCommand(contribuinteId), cancellationToken)))
            .RequirePermission("tributos.gerenciar");
    }

    private static async Task<object> EnviarOk(ISender sender, MediatR.IRequest comando, CancellationToken cancellationToken)
    {
        await sender.Send(comando, cancellationToken).ConfigureAwait(false);
        return new { ok = true };
    }

    private sealed record EntregarDesifPayload(
        int Ano,
        int Mes,
        string FundamentoLegal,
        decimal DeducoesReceita,
        decimal IncentivosFiscais,
        decimal DepositosJudiciais,
        DateOnly VencimentoIssqn,
        IReadOnlyList<SubtituloDesifInput> Subtitulos);

    private sealed record ConcederTituloSimPayload(DateOnly DataRegistro, DateOnly FimVigencia);

    private sealed record AlterarSituacaoSimPayload(AcaoTituloSim Acao, string? Motivo);

    private sealed record AderirDomicilioPayload(DateOnly DataAdesao, int DiasCienciaTacita = DomicilioEletronicoContribuinte.DiasCienciaTacitaPadrao);

    private sealed record DisponibilizarMensagemPayload(
        TipoMensagemFiscal Tipo,
        string Assunto,
        string Corpo,
        DateOnly DataDisponibilizacao,
        int DiasPrazoManifestacao,
        string? ReferenciaExterna);
}
