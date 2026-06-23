using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Transparencia.Application.DeclaracoesFiscais;
using Tensorroot.Gov.Modules.Transparencia.Application.Esic;
using Tensorroot.Gov.Modules.Transparencia.Application.Fiscal;
using Tensorroot.Gov.Modules.Transparencia.Application.PortalPublico;
using Tensorroot.Gov.Modules.Transparencia.Application.RemessasFolha;
using Tensorroot.Gov.Modules.Transparencia.Application.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;
using Tensorroot.Gov.Modules.Transparencia.Domain.Esic;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.PortalPublico;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do modulo Transparencia.</summary>
internal static class TransparenciaEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/transparencia").WithTags("Transparencia");

        MapearRemessasTce(grupo);
        MapearDeclaracoesFiscais(grupo);
        MapearNucleoFiscal(grupo);
        MapearEsicInterno(grupo);
        MapearPortalPublicoConfig(grupo);

        // Superficie PUBLICA anonima (fora de /api): grupo /publico/transparencia/{slug}. Resolve o
        // tenant por slug (sem JWT), fixa o TenantOverride e expoe leitura read-only + e-SIC do cidadao.
        TransparenciaPublicaEndpoints.Map(endpoints);
    }

    /// <summary>
    /// e-SIC INTERNO (autenticado): lista/detalha pedidos do tenant (detalhe COM PII gera trilha de
    /// acesso LG-3) e executa as transicoes de atendimento/resposta/prorrogacao/indeferimento/recurso.
    /// </summary>
    private static void MapearEsicInterno(RouteGroupBuilder grupo)
    {
        var esic = grupo.MapGroup("/esic").WithTags("Transparencia.Esic");

        esic.MapGet("/", async (int? ano, SituacaoPedidoSic? situacao, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListarPedidosSicQuery(ano, situacao), ct)))
            .RequirePermission("transparencia.esic.ver");

        esic.MapGet("/{pedidoId:guid}", async (Guid pedidoId, ISender sender, CancellationToken ct) =>
        {
            var detalhe = await sender.Send(new ObterPedidoSicInternoQuery(pedidoId), ct);
            return detalhe is null ? Results.NotFound() : Results.Ok(detalhe);
        }).RequirePermission("transparencia.esic.ver");

        esic.MapPost("/{pedidoId:guid}/atendimento", async (Guid pedidoId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new IniciarAtendimentoSicCommand(pedidoId), ct);
            return Results.NoContent();
        }).RequirePermission("transparencia.esic.responder");

        esic.MapPost("/{pedidoId:guid}/resposta", async (Guid pedidoId, ResponderSicPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ResponderPedidoSicCommand(pedidoId, payload.Texto, payload.ReferenciaAnexo), ct);
            return Results.NoContent();
        }).RequirePermission("transparencia.esic.responder");

        esic.MapPost("/{pedidoId:guid}/prorrogacao", async (Guid pedidoId, ProrrogarSicPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ProrrogarPedidoSicCommand(pedidoId, payload.Motivo), ct);
            return Results.NoContent();
        }).RequirePermission("transparencia.esic.responder");

        esic.MapPost("/{pedidoId:guid}/indeferimento", async (Guid pedidoId, IndeferirSicPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new IndeferirPedidoSicCommand(pedidoId, payload.FundamentoLegal), ct);
            return Results.NoContent();
        }).RequirePermission("transparencia.esic.responder");

        esic.MapPost("/{pedidoId:guid}/recurso/decisao", async (Guid pedidoId, DecidirRecursoSicPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DecidirRecursoSicCommand(pedidoId, payload.Resultado, payload.Decisao), ct);
            return Results.NoContent();
        }).RequirePermission("transparencia.esic.responder");
    }

    /// <summary>INTERNO: configura o slug/nome do portal publico do tenant (transparencia.gerenciar).</summary>
    private static void MapearPortalPublicoConfig(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/portal-publico", async (ConfigurarPortalPayload payload, ISender sender, CancellationToken ct)
            => Results.Ok(new { slug = await sender.Send(new ConfigurarPortalPublicoCommand(payload.Slug, payload.NomeEnte), ct) }))
            .RequirePermission("transparencia.gerenciar");
    }

    private sealed record ResponderSicPayload(string Texto, string? ReferenciaAnexo);

    private sealed record ProrrogarSicPayload(string Motivo);

    private sealed record IndeferirSicPayload(string FundamentoLegal);

    private sealed record DecidirRecursoSicPayload(ResultadoRecurso Resultado, string Decisao);

    private sealed record ConfigurarPortalPayload(string Slug, string NomeEnte);

    /// <summary>
    /// M7.0 — Núcleo fiscal dos mínimos constitucionais (Saúde 15% ASPS / Educação 25% MDE): registra as
    /// regras de classificação setorial (função/fonte → setor), projeta a execução fiscal (receita-base +
    /// despesas) e apura os indicadores. A apuração é reprodutível (sem relógio — usa o exercício como
    /// âncora) e tenant-scoped (Global Query Filter). Toda escrita é auditada e os percentuais são
    /// parametrizáveis por tenant+vigência (default legal LC 141/CF 212).
    /// </summary>
    private static void MapearNucleoFiscal(RouteGroupBuilder grupo)
    {
        var fiscal = grupo.MapGroup("/fiscal").WithTags("Transparencia.Fiscal");

        // Registra uma regra de classificação setorial versionada (função[, fonte] → setor, computa?).
        fiscal.MapPost("/regras-classificacao", async (
            RegistrarRegraClassificacaoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("transparencia.gerenciar");

        // Projeta linhas de execução fiscal (receita-base + despesas por função/fonte), idempotente por hash.
        fiscal.MapPost("/execucao", async (
            RegistrarExecucaoFiscalCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { registradas = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("transparencia.gerenciar");

        // Apura os mínimos constitucionais do exercício (base/aplicado/%/limite/situação por setor).
        fiscal.MapGet("/minimos", async (
            int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ApurarMinimosQuery(exercicio), cancellationToken)))
            .RequirePermission("transparencia.ver");

        // Apura E PUBLICA os mínimos do exercício (MinimoConstitucionalApuradoIntegrationEvent via Outbox)
        // para o Painel do Gestor exibir o semáforo Saúde/Educação. Mutação (escreve no Outbox).
        fiscal.MapPost("/minimos/publicar", async (
            PublicarMinimosPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new PublicarMinimosConstitucionaisCommand(payload.Exercicio), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("transparencia.gerenciar");
    }

    private sealed record PublicarMinimosPayload(int Exercicio);

    private static void MapearRemessasTce(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/remessas-tce", async (
            GerarRemessaTceCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("transparencia.gerenciar");

        // REMESSA DE FOLHA ao TCE-RS (Res. 1099): monta TCE_4810/4820/4960 a partir do resumo consumido do RH.
        // Reusa o mesmo ciclo da remessa SIAPC (validacao/empacotamento/protocolo) abaixo — mesma RemessaTce.
        grupo.MapPost("/remessas-folha-tce", async (
            GerarRemessaFolhaTceCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("transparencia.gerenciar");

        grupo.MapGet("/remessas-tce/{remessaId:guid}", async (
            Guid remessaId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterRemessaTcePorIdQuery(remessaId), cancellationToken)))
            .RequirePermission("transparencia.ver");

        grupo.MapGet("/remessas-tce", async (
            int exercicio, TipoPeriodo? tipo, SituacaoRemessaTce? situacao, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarRemessasTcePorPeriodoQuery(exercicio, tipo, situacao), cancellationToken)))
            .RequirePermission("transparencia.ver");

        // Pre-validacao LOCAL (e-Validador/RDI): so fica Validada se nao houver erro.
        grupo.MapPost("/remessas-tce/{remessaId:guid}/validacao", async (
            Guid remessaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ValidarRemessaTceCommand(remessaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("transparencia.gerenciar");

        // Relatorio de criticas (RDI) da pre-validacao local.
        grupo.MapGet("/remessas-tce/{remessaId:guid}/criticas", async (
            Guid remessaId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterCriticasRemessaTceQuery(remessaId), cancellationToken)))
            .RequirePermission("transparencia.ver");

        // Empacota o ZIP nomeado e marca ProntaParaTransmissao (NAO transmite — TCE-RS nao tem API de envio).
        grupo.MapPost("/remessas-tce/{remessaId:guid}/empacotamento", async (
            Guid remessaId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { nomeZip = await sender.Send(new EmpacotarRemessaTceCommand(remessaId), cancellationToken) }))
            .RequirePermission("transparencia.gerenciar");

        // Baixa um .TXT componente (?arquivo=NOME.TXT) ou o ZIP completo (sem query).
        grupo.MapGet("/remessas-tce/{remessaId:guid}/arquivo", async (
            Guid remessaId, string? arquivo, ISender sender, CancellationToken cancellationToken) =>
        {
            var download = await sender.Send(new BaixarArquivoRemessaTceQuery(remessaId, arquivo), cancellationToken);
            return download is null
                ? Results.NotFound()
                : Results.File(download.Conteudo.ToArray(), download.ContentType, download.NomeArquivo);
        }).RequirePermission("transparencia.ver");

        // ATO HUMANO: registra o protocolo/recibo retornado pelo PAD/e-Protocolo (gated por SoD).
        grupo.MapPost("/remessas-tce/{remessaId:guid}/protocolo", async (
            Guid remessaId, RegistrarProtocoloPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new RegistrarProtocoloTceCommand(remessaId, payload.Protocolo, payload.DataRecibo),
                cancellationToken);
            return Results.NoContent();
        }).RequirePermission("transparencia.remessa.transmitir");

        grupo.MapPost("/remessas-tce/{remessaId:guid}/homologacao", async (
            Guid remessaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new HomologarRemessaTceCommand(remessaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("transparencia.gerenciar");

        grupo.MapPost("/remessas-tce/{remessaId:guid}/vencimento-prazo", async (
            Guid remessaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new VencerPrazoRemessaTceCommand(remessaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("transparencia.gerenciar");
    }

    private sealed record RegistrarProtocoloPayload(string Protocolo, DateOnly DataRecibo);

    private static void MapearDeclaracoesFiscais(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/declaracoes-fiscais", async (
            ConsolidarDeclaracaoFiscalCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("transparencia.gerenciar");

        grupo.MapGet("/declaracoes-fiscais/{declaracaoId:guid}", async (
            Guid declaracaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterDeclaracaoFiscalPorIdQuery(declaracaoId), cancellationToken)))
            .RequirePermission("transparencia.ver");

        grupo.MapGet("/declaracoes-fiscais", async (
            int exercicio, TipoDeclaracaoFiscal? tipo, SituacaoDeclaracaoFiscal? situacao, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarDeclaracoesFiscaisPorExercicioQuery(exercicio, tipo, situacao), cancellationToken)))
            .RequirePermission("transparencia.ver");

        grupo.MapPost("/declaracoes-fiscais/{declaracaoId:guid}/transmissao", async (
            Guid declaracaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new TransmitirDeclaracaoFiscalCommand(declaracaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("transparencia.gerenciar");

        grupo.MapPost("/declaracoes-fiscais/{declaracaoId:guid}/homologacao", async (
            Guid declaracaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new HomologarDeclaracaoFiscalCommand(declaracaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("transparencia.gerenciar");

        grupo.MapPost("/declaracoes-fiscais/{declaracaoId:guid}/rejeicao", async (
            Guid declaracaoId, RejeitarDeclaracaoFiscalPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RejeitarDeclaracaoFiscalCommand(declaracaoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("transparencia.gerenciar");

        // Gera a MSC zipada (CSV adaptado do XBRL-GL) para upload MANUAL no portal SICONFI.
        grupo.MapPost("/declaracoes-fiscais/{declaracaoId:guid}/msc", async (
            Guid declaracaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            var msc = await sender.Send(new GerarMscCommand(declaracaoId), cancellationToken);
            return Results.File(msc.Conteudo.ToArray(), msc.ContentType, msc.NomeArquivo);
        }).RequirePermission("transparencia.gerenciar");

        // Reconciliacao via API de Dados Abertos do SICONFI (SOMENTE consulta — nao e envio).
        // Degradacao graciosa: se a API externa nao responder (apos Polly), retorna 503 (indisponivel),
        // nao 500 — nao e erro do ente nem do sistema; o operador reexecuta mais tarde.
        grupo.MapPost("/declaracoes-fiscais/{declaracaoId:guid}/reconciliacao", async (
            Guid declaracaoId, ReconciliarPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            var resultado = await sender.Send(new ReconciliarSiconfiCommand(declaracaoId, payload.IdEnte), cancellationToken);
            return resultado.Indisponivel
                ? Results.Json(resultado, statusCode: StatusCodes.Status503ServiceUnavailable)
                : Results.Ok(resultado);
        }).RequirePermission("transparencia.ver");
    }

    private sealed record RejeitarDeclaracaoFiscalPayload(string Motivo);

    private sealed record ReconciliarPayload(string IdEnte);
}
