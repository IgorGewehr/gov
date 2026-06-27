using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.TempoServico;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;

/// <summary>
/// Endpoints HTTP da Certidao de Tempo de Servico/Contribuicao (CTC): emissao com apuracao do tempo do
/// servidor, ficha por servidor, detalhe do documento, validacao publica por codigo e anulacao.
/// </summary>
internal static partial class RecursosHumanosEndpoints
{
    private static void MapearTempoServico(RouteGroupBuilder grupo)
    {
        var certidoes = grupo.MapGroup("/certidoes-tempo").WithTags("RecursosHumanos.CertidaoTempoServico");

        // EMISSAO: apura o efetivo exercicio do vinculo + periodos averbados informados; numera e autentica.
        certidoes.MapPost("/", async (
            EmitirCertidaoTempoServicoCommand comando, ISender sender, CancellationToken ct)
            => Results.Ok(new { id = await sender.Send(comando, ct) }))
            .RequirePermission("recursoshumanos.gerenciar");

        // FICHA: certidoes de tempo emitidas para um servidor (mais recentes primeiro).
        certidoes.MapGet("/servidores/{servidorId:guid}", async (
            Guid servidorId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ListarCertidoesDoServidorQuery(servidorId), ct)))
            .RequirePermission("recursoshumanos.ver");

        // DETALHE do documento (periodos computados, totais, codigo de autenticacao).
        certidoes.MapGet("/{certidaoId:guid}", async (
            Guid certidaoId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ObterCertidaoQuery(certidaoId), ct)))
            .RequirePermission("recursoshumanos.ver");

        // VALIDACAO PUBLICA por codigo de autenticacao (servico de balcao — confirma autenticidade).
        certidoes.MapGet("/validar/{codigo}", async (
            string codigo, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ValidarCertidaoQuery(codigo), ct)))
            .RequirePermission("recursoshumanos.ver");

        // ANULACAO (torna sem efeito; numeracao consumida preservada).
        certidoes.MapPost("/{certidaoId:guid}/anulacao", async (
            Guid certidaoId, AnularCertidaoPayload payload, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AnularCertidaoTempoServicoCommand(certidaoId, payload.Motivo), ct);
            return Results.NoContent();
        })
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private sealed record AnularCertidaoPayload(string Motivo);
}
