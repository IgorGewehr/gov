using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.SicapPessoal;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;

/// <summary>
/// Endpoints da remessa de auditoria de pessoal ao TCE-RS (SICAP-AP / SIAPESweb): abertura do lote,
/// inclusao de atos de admissao (auto-preenchidos a partir do servidor), geracao do arquivo de
/// importacao (leiaute estadual 57 posicoes) e marcacao de transmissao. Transmissao real = M10.
/// </summary>
internal static partial class RecursosHumanosEndpoints
{
    private static void MapearSicapPessoal(RouteGroupBuilder grupo)
    {
        var sicap = grupo.MapGroup("/sicap-pessoal").WithTags("RecursosHumanos.SicapPessoal");

        // Abre uma remessa de pessoal (sequencial do lote apurado por orgao no backend).
        sicap.MapPost("/remessas", async (
            AbrirRemessaSicapCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Lista as remessas do tenant (filtro opcional por situacao).
        sicap.MapGet("/remessas", async (
            SituacaoRemessaSicap? situacao, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListarRemessasSicapQuery(situacao), ct)))
            .RequirePermission("recursoshumanos.ver");

        // Detalhe de uma remessa (cabecalho + atos do corpo, CPF mascarado).
        sicap.MapGet("/remessas/{remessaId:guid}", async (
            Guid remessaId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ObterRemessaSicapQuery(remessaId), ct)))
            .RequirePermission("recursoshumanos.ver");

        // Inclui um ato de admissao na remessa, auto-preenchido a partir de um servidor.
        sicap.MapPost("/remessas/{remessaId:guid}/atos", async (
            Guid remessaId, AtoDeServidorPayload payload, ISender sender, CancellationToken ct)
            => Results.Ok(new
            {
                id = await sender.Send(
                    new AdicionarAtoDeServidorCommand(remessaId, payload.ServidorId, payload.TituloOverride, payload.RegimeOverride, payload.ClassificacaoConcurso), ct),
            }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Gera (fecha) a remessa e devolve o arquivo de importacao SIAPES (download).
        sicap.MapPost("/remessas/{remessaId:guid}/geracao", async (
            Guid remessaId, ISender sender, CancellationToken ct) =>
        {
            var artefato = await sender.Send(new GerarRemessaSicapCommand(remessaId), ct);
            return Results.File(artefato.Conteudo, "text/plain", artefato.NomeArquivo);
        })
            .RequirePermission("recursoshumanos.gerenciar");

        // Marca a remessa como transmitida ao TCE-RS (registra o protocolo). // TODO(M10): envio real.
        sicap.MapPost("/remessas/{remessaId:guid}/transmissao", async (
            Guid remessaId, TransmissaoSicapPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new TransmitirRemessaSicapCommand(remessaId, payload.Protocolo), ct);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private sealed record AtoDeServidorPayload(
        Guid ServidorId,
        TipoAtoAdmissao? TituloOverride,
        RegimeJuridicoSiapes? RegimeOverride,
        int? ClassificacaoConcurso);

    private sealed record TransmissaoSicapPayload(string Protocolo);
}
