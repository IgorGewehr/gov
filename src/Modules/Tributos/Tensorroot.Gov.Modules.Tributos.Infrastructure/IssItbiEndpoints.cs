using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Tributos.Application.Iss;
using Tensorroot.Gov.Modules.Tributos.Application.Itbi;
using Tensorroot.Gov.Modules.Tributos.Application.Nfse;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) do ISS (apuração sobre NFS-e ingeridas — passiva) e do ITBI
/// (guia avulsa por transmissão). RBAC por permissões tributos.* (negar por padrão — CLAUDE.md §6).
/// </summary>
internal static class IssItbiEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/tributos").WithTags("Tributos.IssItbi");

        // ISS — ingestão (sob demanda) das NFS-e do ADN para o tenant atual. Integração PASSIVA
        // (ADR-0003 / CLAUDE.md §8): apenas baixamos/deduplicamos do Ambiente Nacional — não emitimos.
        // Espelha a lógica do Worker NfseSync; útil para gestão e para provar a ingestão por HTTP.
        grupo.MapPost("/iss/nfse/sincronizar", async (
            SincronizarNfsePayload payload, INfseSincronizador sincronizador, CancellationToken cancellationToken)
            => Results.Ok(new { importadas = await sincronizador.SincronizarAsync(payload.Prestadores, payload.Desde, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        // ISS — tabela de alíquotas por item LC 116 (lei municipal parametrizável).
        grupo.MapPost("/iss/aliquotas", async (
            ConfigurarTabelaAliquotaIssCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        // ISS — apuração mensal (livro eletrônico) de um contribuinte a partir das NFS-e ingeridas.
        grupo.MapPost("/iss/contribuintes/{contribuinteId:guid}/apurar", async (
            Guid contribuinteId, ApurarIssPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new ApurarIssMensalCommand(contribuinteId, payload.Ano, payload.Mes, payload.VencimentoIssProprio),
                cancellationToken)))
            .RequirePermission("tributos.gerenciar");

        // ISS — GIA mensal: declaração do PRÓPRIO prestador (serviços prestados, base, ISS devido). Cobre
        // serviços SEM NFS-e nacional, que a apuração derivada do ADN não alcança (PARIDADE-PoC SW-A10).
        // Constitui o crédito do ISS próprio (CTN art. 150 — lançamento por homologação).
        grupo.MapPost("/iss/contribuintes/{contribuinteId:guid}/gia", async (
            Guid contribuinteId, EntregarGiaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new EntregarGiaIssCommand(
                    contribuinteId, payload.Ano, payload.Mes, payload.FundamentoLegal, payload.VencimentoIssDevido, payload.Servicos),
                cancellationToken)))
            .RequirePermission("tributos.gerenciar");

        // ITBI — alíquota por exercício (lei municipal parametrizável).
        grupo.MapPost("/itbi/aliquotas", async (
            ConfigurarAliquotaItbiCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.gerenciar");

        // ITBI — apuração (preview) de uma transmissão: base = VALOR DECLARADO (Tema 1.113/STJ); o valor
        // venal de referência só dispara o alerta de triagem.
        grupo.MapGet("/itbi/imoveis/{imovelId:guid}/preview", async (
            Guid imovelId, int exercicio, decimal valorDeclarado, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new CalcularItbiQuery(imovelId, exercicio, valorDeclarado), cancellationToken)))
            .RequirePermission("tributos.ver");

        // ITBI — registra a transmissão, lança o imposto pela base declarada e gera a guia avulsa (DAM).
        grupo.MapPost("/itbi/lancar", async (
            LancarItbiCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(comando, cancellationToken)))
            .RequirePermission("tributos.gerenciar");

        MapArbitramento(grupo);
    }

    // ITBI — ARBITRAMENTO da base de cálculo (CTN art. 148): processo SEPARADO, AUDITADO e com
    // CONTRADITÓRIO. Sob o Tema 1.113/STJ a base declarada presume-se verdadeira; só este processo
    // pode afastá-la (nunca de ofício por valor de referência). Verbo fino "tributos.itbi.arbitrar".
    private static void MapArbitramento(RouteGroupBuilder grupo)
    {
        // Instaurar o processo de arbitramento (não altera a guia já emitida pelo declarado).
        grupo.MapPost("/itbi/arbitramento/instaurar", async (
            InstaurarArbitramentoItbiCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("tributos.itbi.arbitrar");

        // Abrir o contraditório (notificar o contribuinte) — passo obrigatório.
        grupo.MapPost("/itbi/arbitramento/{processoId:guid}/contraditorio/abrir", async (
            Guid processoId, AbrirContraditorioPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await EnviarOk(sender, new AbrirContraditorioArbitramentoItbiCommand(processoId, payload.DataNotificacao), cancellationToken)))
            .RequirePermission("tributos.itbi.arbitrar");

        // Registrar a defesa/avaliação contraditória do contribuinte.
        grupo.MapPost("/itbi/arbitramento/{processoId:guid}/contraditorio/registrar", async (
            Guid processoId, RegistrarContraditorioPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await EnviarOk(sender, new RegistrarContraditorioArbitramentoItbiCommand(processoId, payload.Justificativa, payload.DataApresentacao), cancellationToken)))
            .RequirePermission("tributos.itbi.arbitrar");

        // Concluir o arbitramento: aplica a base arbitrada e lança a diferença de ofício (complementar).
        grupo.MapPost("/itbi/arbitramento/{processoId:guid}/concluir", async (
            Guid processoId, ConcluirArbitramentoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new ConcluirArbitramentoItbiCommand(processoId, payload.AdquirenteId, payload.ValorArbitrado, payload.DataDecisao, payload.VencimentoComplementar),
                cancellationToken)))
            .RequirePermission("tributos.itbi.arbitrar");

        // Cancelar o arbitramento (a declaração do contribuinte prevaleceu).
        grupo.MapPost("/itbi/arbitramento/{processoId:guid}/cancelar", async (
            Guid processoId, CancelarArbitramentoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await EnviarOk(sender, new CancelarArbitramentoItbiCommand(processoId, payload.DataCancelamento), cancellationToken)))
            .RequirePermission("tributos.itbi.arbitrar");
    }

    private static async Task<object> EnviarOk(ISender sender, MediatR.IRequest comando, CancellationToken cancellationToken)
    {
        await sender.Send(comando, cancellationToken).ConfigureAwait(false);
        return new { ok = true };
    }

    private sealed record AbrirContraditorioPayload(DateOnly DataNotificacao);

    private sealed record RegistrarContraditorioPayload(string Justificativa, DateOnly DataApresentacao);

    private sealed record ConcluirArbitramentoPayload(Guid AdquirenteId, decimal ValorArbitrado, DateOnly DataDecisao, DateOnly VencimentoComplementar);

    private sealed record CancelarArbitramentoPayload(DateOnly DataCancelamento);

    private sealed record ApurarIssPayload(int Ano, int Mes, DateOnly VencimentoIssProprio);

    private sealed record EntregarGiaPayload(
        int Ano,
        int Mes,
        string FundamentoLegal,
        DateOnly VencimentoIssDevido,
        IReadOnlyList<ServicoGiaInput> Servicos);

    private sealed record SincronizarNfsePayload(IReadOnlyList<string> Prestadores, DateOnly Desde);
}
