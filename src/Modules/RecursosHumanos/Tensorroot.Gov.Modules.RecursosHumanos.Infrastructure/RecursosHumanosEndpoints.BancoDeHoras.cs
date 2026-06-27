using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.BancoDeHoras;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;

/// <summary>Endpoints HTTP do BANCO DE HORAS (saldo vivo, lancamentos, prescricao).</summary>
internal static partial class RecursosHumanosEndpoints
{
    private static void MapearBancoDeHoras(RouteGroupBuilder grupo)
    {
        // BANCO DE HORAS: saldo vivo por servidor alimentado pelas apuracoes de ponto fechadas + ajustes
        // manuais; a rotina de prescricao zera os creditos fora da janela parametrizavel (6/12 meses).
        var banco = grupo.MapGroup("/banco-de-horas").WithTags("RecursosHumanos.BancoDeHoras");

        // Extrato (saldo + livro-razao) do banco de horas do servidor.
        banco.MapGet("/servidores/{servidorId:guid}", async (
            Guid servidorId, ISender sender, CancellationToken ct)
            => Results.Ok(await sender.Send(new ObterExtratoBancoDeHorasQuery(servidorId), ct)))
            .RequirePermission("recursoshumanos.ver");

        // Lancamento manual (credito/debito) — ajuste administrativo/compensacao formal. Devolve o saldo.
        banco.MapPost("/servidores/{servidorId:guid}/lancamentos", async (
            Guid servidorId, LancamentoBancoHorasPayload payload, ISender sender, CancellationToken ct)
            => Results.Ok(new
            {
                saldoMinutos = await sender.Send(
                    new LancarBancoDeHorasCommand(servidorId, payload.Minutos, payload.Credito, payload.Data, payload.Descricao),
                    ct),
            }))
            .RequirePermission("recursoshumanos.gerenciar");

        // Rotina de prescricao em lote (zera creditos fora da janela). Devolve o total de minutos prescritos.
        banco.MapPost("/prescricao", async (ISender sender, CancellationToken ct)
            => Results.Ok(new { minutosPrescritos = await sender.Send(new ExecutarPrescricaoBancoDeHorasCommand(), ct) }))
            .RequirePermission("recursoshumanos.gerenciar");
    }

    private sealed record LancamentoBancoHorasPayload(int Minutos, bool Credito, DateOnly Data, string Descricao);
}
