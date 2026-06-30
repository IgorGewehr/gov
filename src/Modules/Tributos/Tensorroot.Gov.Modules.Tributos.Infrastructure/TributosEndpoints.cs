using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Tributos.Application.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Application.Dividas;
using Tensorroot.Gov.Modules.Tributos.Application.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do módulo Tributos — núcleo + Dívida Ativa/CDA/protesto/execução.</summary>
internal static class TributosEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/tributos").WithTags("Tributos");

        grupo.MapPost("/contribuintes/pessoa-fisica", async (
            CadastrarContribuintePessoaFisicaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        // Picker do balcão (P1): busca por nome ou CPF/CNPJ. Sem termo, retorna os primeiros (teto no handler).
        grupo.MapGet("/contribuintes", async (
            string? termo, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarContribuintesQuery(termo), cancellationToken)))
            .RequirePermission("tributos.ver");

        grupo.MapPost("/lancamentos", async (
            LancarCreditoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        // 1) INSCRIÇÃO EM DÍVIDA ATIVA — a partir de lançamento VENCIDO e não pago (qualquer espécie).
        grupo.MapPost("/lancamentos/{lancamentoId:guid}/inscrever-divida-ativa", async (
            Guid lancamentoId, InscreverDividaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                dividaAtivaId = await sender.Send(
                    new InscreverEmDividaAtivaCommand(
                        lancamentoId,
                        payload.FundamentoLegal,
                        payload.MultaMoraPercentual,
                        payload.JurosMoraPercentualMensal,
                        payload.CorrecaoPercentualMensal,
                        payload.FundamentoEncargos,
                        payload.DataConstituicaoDefinitiva,
                        payload.DataInscricao,
                        payload.AnosPrescricao ?? DividaAtiva.AnosPrescricao),
                    cancellationToken)
            }))
            .RequirePermission("tributos.gerenciar");

        // 2) CDA — requisitos legais obrigatórios (LEF art. 2º §5º); recusa se faltar requisito.
        grupo.MapPost("/dividas/{dividaAtivaId:guid}/cda", async (
            Guid dividaAtivaId, EmitirCdaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new EmitirCdaCommand(
                    dividaAtivaId,
                    payload.NumeroCda,
                    payload.DataBaseEncargos,
                    payload.DomicilioDevedor,
                    payload.CoResponsaveis,
                    payload.ProcessoAdministrativo),
                cancellationToken)))
            .RequirePermission("tributos.gerenciar");

        // 3) COBRANÇA — PROTESTO extrajudicial (remessa/retorno ao CRA via ACL).
        grupo.MapPost("/dividas/{dividaAtivaId:guid}/protesto/remessa", async (
            Guid dividaAtivaId, ProtestoRemessaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new GerarRemessaProtestoCommand(dividaAtivaId, payload.DataGeracao), cancellationToken)))
            .RequirePermission("tributos.gerenciar");

        grupo.MapPost("/dividas/{dividaAtivaId:guid}/protesto/retorno", async (
            Guid dividaAtivaId, ProtestoRetornoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new ProcessarRetornoProtestoCommand(
                    dividaAtivaId,
                    payload.RemessaProtestoId,
                    payload.Ocorrencia,
                    payload.DataRetorno,
                    payload.ProtocoloCartorio),
                cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("tributos.gerenciar");

        // 3) COBRANÇA — gancho de EXECUÇÃO FISCAL (gera/exporta CDA + petição).
        grupo.MapPost("/dividas/{dividaAtivaId:guid}/execucao-fiscal", async (
            Guid dividaAtivaId, ExecucaoFiscalPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AjuizarExecucaoFiscalCommand(dividaAtivaId, payload.DataAjuizamento), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("tributos.gerenciar");

        // 4) PRESCRIÇÃO — avalia prazo (CTN art. 174) e encargos numa data de referência; sinaliza prescrita.
        grupo.MapGet("/dividas/{dividaAtivaId:guid}/prescricao", async (
            Guid dividaAtivaId, DateOnly dataReferencia, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new AvaliarPrescricaoDividaQuery(dividaAtivaId, dataReferencia), cancellationToken)))
            .RequirePermission("tributos.ver");

        grupo.MapGet("/contribuintes/{contribuinteId:guid}/dividas-ativas", async (
            Guid contribuinteId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterDividasAtivasDoContribuinteQuery(contribuinteId), cancellationToken)))
            .RequirePermission("tributos.ver");

        // 5) QUITAÇÃO — baixa a dívida e publica (Outbox) a receita arrecadada + a posição da dívida ativa
        // do exercício (consumidos por Finanças e pelo Painel do Gestor). Antes inalcançável por HTTP.
        grupo.MapPost("/dividas/{dividaAtivaId:guid}/quitar", async (
            Guid dividaAtivaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new QuitarDividaCommand(dividaAtivaId), cancellationToken);
            return Results.NoContent();
        })
            .RequirePermission("tributos.gerenciar");
    }

    private sealed record InscreverDividaPayload(
        string FundamentoLegal,
        decimal MultaMoraPercentual,
        decimal JurosMoraPercentualMensal,
        decimal CorrecaoPercentualMensal,
        string FundamentoEncargos,
        DateOnly? DataConstituicaoDefinitiva = null,
        DateOnly? DataInscricao = null,
        int? AnosPrescricao = null);

    private sealed record EmitirCdaPayload(
        string NumeroCda,
        DateOnly DataBaseEncargos,
        string? DomicilioDevedor = null,
        string? CoResponsaveis = null,
        string? ProcessoAdministrativo = null);

    private sealed record ProtestoRemessaPayload(DateOnly DataGeracao);

    private sealed record ProtestoRetornoPayload(
        Guid RemessaProtestoId,
        OcorrenciaProtesto Ocorrencia,
        DateOnly DataRetorno,
        string? ProtocoloCartorio = null);

    private sealed record ExecucaoFiscalPayload(DateOnly DataAjuizamento);
}
