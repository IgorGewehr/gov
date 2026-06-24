using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Convenios.Application.Mrosc;
using Tensorroot.Gov.Modules.Convenios.Application.Recebidos;
using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;
using Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;

namespace Tensorroot.Gov.Modules.Convenios.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) do modulo Convenios — DOIS fluxos separados sob <c>/api/convenios</c>:
/// <c>/recebidos</c> (convenios federais — fluxo A) e <c>/parcerias</c> (MROSC — fluxo B).
/// </summary>
internal static class ConveniosEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/convenios").WithTags("Convenios");
        MapearRecebidos(grupo);
        MapearParcerias(grupo);
    }

    // ===== Fluxo A — Convenios federais RECEBIDOS =====
    private static void MapearRecebidos(RouteGroupBuilder grupo)
    {
        var recebidos = grupo.MapGroup("/recebidos").WithTags("Convenios.Recebidos");

        recebidos.MapGet("/", async (SituacaoConvenioRecebido? situacao, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListarConveniosQuery(situacao), ct)))
            .RequirePermission("convenios.ver");

        recebidos.MapGet("/{convenioId:guid}", async (Guid convenioId, ISender sender, CancellationToken ct) =>
        {
            var detalhe = await sender.Send(new ObterConvenioQuery(convenioId), ct);
            return detalhe is null ? Results.NotFound() : Results.Ok(detalhe);
        }).RequirePermission("convenios.ver");

        recebidos.MapPost("/", async (RegistrarPropostaConvenioCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("convenios.gerenciar");

        recebidos.MapPost("/{convenioId:guid}/plano/aprovar", async (Guid convenioId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AprovarPlanoTrabalhoConvenioCommand(convenioId), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        recebidos.MapPost("/{convenioId:guid}/celebrar", async (Guid convenioId, CelebrarConvenioPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new CelebrarConvenioCommand(
                convenioId, payload.VigenciaInicio, payload.VigenciaFim, payload.ModalidadeContrapartida,
                payload.ValorContrapartida, payload.NumeroConvenioTransferegov, payload.ClassificacaoSugerida), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        recebidos.MapPost("/{convenioId:guid}/parcelas/{numeroOrdem:int}/liberar", async (Guid convenioId, int numeroOrdem, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RegistrarLiberacaoParcelaConvenioCommand(convenioId, numeroOrdem), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        recebidos.MapPost("/{convenioId:guid}/rendimentos", async (Guid convenioId, RendimentoPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RegistrarRendimentoConvenioCommand(convenioId, payload.Valor, payload.Data), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        recebidos.MapPost("/{convenioId:guid}/prestacoes/parcial", async (Guid convenioId, PrestacaoParcialPayload payload, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(new AbrirPrestacaoParcialConvenioCommand(convenioId, payload.NumeroEtapa, payload.CompetenciaRef), ct) }))
            .RequirePermission("convenios.gerenciar");

        recebidos.MapPost("/{convenioId:guid}/prestacoes/final", async (Guid convenioId, CompetenciaPayload payload, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(new AbrirPrestacaoFinalConvenioCommand(convenioId, payload.CompetenciaRef), ct) }))
            .RequirePermission("convenios.gerenciar");

        recebidos.MapPost("/{convenioId:guid}/prestacoes/{prestacaoId:guid}/submeter", async (Guid convenioId, Guid prestacaoId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new SubmeterPrestacaoConvenioCommand(convenioId, prestacaoId), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        recebidos.MapPost("/{convenioId:guid}/prestacoes/{prestacaoId:guid}/analise/iniciar", async (Guid convenioId, Guid prestacaoId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new IniciarAnaliseConvenioCommand(convenioId, prestacaoId), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        recebidos.MapPost("/{convenioId:guid}/prestacoes/{prestacaoId:guid}/saneamento", async (Guid convenioId, Guid prestacaoId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AbrirSaneamentoConvenioCommand(convenioId, prestacaoId), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        recebidos.MapPost("/{convenioId:guid}/prestacoes/{prestacaoId:guid}/analise/retomar", async (Guid convenioId, Guid prestacaoId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RetomarAnaliseConvenioCommand(convenioId, prestacaoId), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        recebidos.MapPost("/{convenioId:guid}/prestacoes/{prestacaoId:guid}/analise/concluir", async (Guid convenioId, Guid prestacaoId, ResultadoPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ConcluirAnaliseConvenioCommand(convenioId, prestacaoId, payload.Resultado), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        recebidos.MapPost("/{convenioId:guid}/inadimplencia", async (Guid convenioId, MotivoPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeclararInadimplenciaConvenioCommand(convenioId, payload.Motivo), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");
    }

    // ===== Fluxo B — Parcerias-saida OSC (MROSC) =====
    private static void MapearParcerias(RouteGroupBuilder grupo)
    {
        var parcerias = grupo.MapGroup("/parcerias").WithTags("Convenios.Mrosc");

        parcerias.MapGet("/", async (SituacaoParceriaOsc? situacao, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListarParceriasQuery(situacao), ct)))
            .RequirePermission("convenios.ver");

        parcerias.MapGet("/{parceriaId:guid}", async (Guid parceriaId, ISender sender, CancellationToken ct) =>
        {
            var detalhe = await sender.Send(new ObterParceriaQuery(parceriaId), ct);
            return detalhe is null ? Results.NotFound() : Results.Ok(detalhe);
        }).RequirePermission("convenios.ver");

        parcerias.MapPost("/", async (IniciarSelecaoParceriaCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("convenios.gerenciar");

        parcerias.MapPost("/{parceriaId:guid}/chamamento/homologar", async (Guid parceriaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new HomologarChamamentoParceriaCommand(parceriaId), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        parcerias.MapPost("/{parceriaId:guid}/plano", async (Guid parceriaId, RegistrarPlanoParceriaPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RegistrarPlanoTrabalhoParceriaCommand(
                parceriaId, payload.Objeto, payload.ValorGlobal, payload.Metas, payload.Parcelas), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        parcerias.MapPost("/{parceriaId:guid}/plano/aprovar", async (Guid parceriaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AprovarPlanoTrabalhoParceriaCommand(parceriaId), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        parcerias.MapPost("/{parceriaId:guid}/celebrar", async (Guid parceriaId, CelebrarParceriaPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new CelebrarParceriaCommand(
                parceriaId, payload.VigenciaInicio, payload.VigenciaFim, payload.GestorParceriaId,
                payload.ComissaoMonitoramentoId, payload.ClassificacaoSugerida), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        parcerias.MapPost("/{parceriaId:guid}/repasses/{numeroOrdem:int}/execucao", async (Guid parceriaId, int numeroOrdem, VincularExecucaoPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new VincularExecucaoRepasseCommand(
                parceriaId, numeroOrdem, payload.EmpenhoId, payload.LiquidacaoId, payload.PagamentoId), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        parcerias.MapPost("/{parceriaId:guid}/repasses/{numeroOrdem:int}/liberar", async (Guid parceriaId, int numeroOrdem, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new LiberarRepasseParceriaCommand(parceriaId, numeroOrdem), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        parcerias.MapPost("/{parceriaId:guid}/prestacao/abrir", async (Guid parceriaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AbrirPrestacaoOscCommand(parceriaId), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        parcerias.MapPost("/{parceriaId:guid}/prestacao/prorrogar", async (Guid parceriaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ProrrogarEntregaPrestacaoOscCommand(parceriaId), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        parcerias.MapPost("/{parceriaId:guid}/prestacao/receber", async (Guid parceriaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ReceberPrestacaoOscCommand(parceriaId), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        parcerias.MapPost("/{parceriaId:guid}/prestacao/analise/iniciar", async (Guid parceriaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new IniciarAnaliseParceriaCommand(parceriaId), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        parcerias.MapPost("/{parceriaId:guid}/prestacao/saneamento", async (Guid parceriaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AbrirSaneamentoParceriaCommand(parceriaId), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        parcerias.MapPost("/{parceriaId:guid}/prestacao/analise/retomar", async (Guid parceriaId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RetomarAnaliseParceriaCommand(parceriaId), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        parcerias.MapPost("/{parceriaId:guid}/prestacao/analise/concluir", async (Guid parceriaId, ResultadoPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ConcluirAnaliseParceriaCommand(parceriaId, payload.Resultado), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");

        parcerias.MapPost("/{parceriaId:guid}/inadimplencia", async (Guid parceriaId, MotivoPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeclararInadimplenciaParceriaCommand(parceriaId, payload.Motivo), ct);
            return Results.NoContent();
        }).RequirePermission("convenios.gerenciar");
    }
}

/// <summary>Payload de celebracao de convenio recebido.</summary>
/// <param name="VigenciaInicio">Inicio da vigencia.</param>
/// <param name="VigenciaFim">Fim da vigencia.</param>
/// <param name="ModalidadeContrapartida">Modalidade da contrapartida.</param>
/// <param name="ValorContrapartida">Valor da contrapartida.</param>
/// <param name="NumeroConvenioTransferegov">Numero no Transferegov.</param>
/// <param name="ClassificacaoSugerida">Classificacao orcamentaria sugerida.</param>
internal sealed record CelebrarConvenioPayload(
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim,
    ModalidadeContrapartida ModalidadeContrapartida,
    decimal ValorContrapartida,
    string NumeroConvenioTransferegov,
    string? ClassificacaoSugerida);

/// <summary>Payload de rendimento.</summary>
/// <param name="Valor">Valor do rendimento.</param>
/// <param name="Data">Data do rendimento.</param>
internal sealed record RendimentoPayload(decimal Valor, DateOnly Data);

/// <summary>Payload de competencia/etapa (abertura de PC final).</summary>
/// <param name="CompetenciaRef">Competencia/etapa de referencia.</param>
internal sealed record CompetenciaPayload(string CompetenciaRef);

/// <summary>Payload de abertura de PC parcial: numero estruturado da etapa (A-INV-4) + competencia descritiva.</summary>
/// <param name="NumeroEtapa">Numero de ordem da etapa/parcela coberta (correlacao deterministica — A-INV-4).</param>
/// <param name="CompetenciaRef">Competencia/etapa de referencia (texto livre descritivo).</param>
internal sealed record PrestacaoParcialPayload(int NumeroEtapa, string CompetenciaRef);

/// <summary>Payload de resultado de analise.</summary>
/// <param name="Resultado">Resultado (aprovada/ressalva/rejeitada).</param>
internal sealed record ResultadoPayload(ResultadoAnalise Resultado);

/// <summary>Payload de motivo (inadimplencia).</summary>
/// <param name="Motivo">Motivo.</param>
internal sealed record MotivoPayload(string Motivo);

/// <summary>Payload de registro de plano de trabalho da parceria.</summary>
/// <param name="Objeto">Objeto.</param>
/// <param name="ValorGlobal">Valor global.</param>
/// <param name="Metas">Metas.</param>
/// <param name="Parcelas">Cronograma de desembolso.</param>
internal sealed record RegistrarPlanoParceriaPayload(
    string Objeto,
    decimal ValorGlobal,
    IReadOnlyList<MetaPayload> Metas,
    IReadOnlyList<ParcelaRepassePayload> Parcelas);

/// <summary>Payload de celebracao de parceria.</summary>
/// <param name="VigenciaInicio">Inicio da vigencia.</param>
/// <param name="VigenciaFim">Fim da vigencia.</param>
/// <param name="GestorParceriaId">Gestor designado.</param>
/// <param name="ComissaoMonitoramentoId">Comissao de monitoramento.</param>
/// <param name="ClassificacaoSugerida">Classificacao orcamentaria sugerida.</param>
internal sealed record CelebrarParceriaPayload(
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim,
    Guid GestorParceriaId,
    Guid ComissaoMonitoramentoId,
    string? ClassificacaoSugerida);

/// <summary>Payload de vinculo de execucao orcamentaria de um repasse.</summary>
/// <param name="EmpenhoId">Empenho.</param>
/// <param name="LiquidacaoId">Liquidacao.</param>
/// <param name="PagamentoId">Pagamento.</param>
internal sealed record VincularExecucaoPayload(Guid? EmpenhoId, Guid? LiquidacaoId, Guid? PagamentoId);
