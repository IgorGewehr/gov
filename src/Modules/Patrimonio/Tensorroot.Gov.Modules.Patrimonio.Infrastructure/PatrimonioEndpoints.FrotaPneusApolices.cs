using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Patrimonio.Application.Frota.Apolices;
using Tensorroot.Gov.Modules.Patrimonio.Application.Frota.Pneus;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) da gestão de PNEUS e APÓLICES da frota (Onda 3a): pneu como item
/// controlado individualmente (estoque → instalação/rodízio por posição de eixo → recapagem → descarte;
/// sulco mínimo legal parametrizável — CONTRAN/CTB) e apólice de seguro do veículo (contratação →
/// renovação → cancelamento; alerta de vencimento). Extraído como partial para manter o arquivo
/// principal dentro do limite de manutenibilidade.
/// </summary>
internal static partial class PatrimonioEndpoints
{
    private static void MapearPneus(RouteGroupBuilder grupo)
    {
        // Cadastra um pneu (ingressa em estoque).
        grupo.MapPost("/pneus", async (
            CadastrarPneuCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("patrimonio.gerenciar");

        // Busca/lista paginada por numero de fogo/marca/modelo/medida, filtro por situacao.
        grupo.MapGet("/pneus", async (
            string? termo, SituacaoPneu? situacao, int? pagina, int? tamanho,
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new BuscarPneusQuery(termo, situacao, pagina ?? 1, tamanho ?? 20), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        // Pneus em rodagem no limite (ou abaixo) do sulco minimo legal (parametrizado por tenant).
        grupo.MapGet("/pneus/no-limite-sulco", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarPneusNoLimiteDeSulcoQuery(), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        // Detalhe de um pneu.
        grupo.MapGet("/pneus/{pneuId:guid}", async (
            Guid pneuId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterPneuQuery(pneuId), cancellationToken) is { } pneu
                ? Results.Ok(pneu)
                : Results.NotFound())
            .RequirePermission("patrimonio.ver");

        // Layout de pneus instalados num veiculo (posicionamento por eixo/lado).
        grupo.MapGet("/veiculos/{veiculoId:guid}/pneus", async (
            Guid veiculoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterLayoutPneusDoVeiculoQuery(veiculoId), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        // Instala o pneu numa posicao (eixo/lado) de um veiculo.
        grupo.MapPost("/pneus/{pneuId:guid}/instalacao", async (
            Guid pneuId, InstalarPneuPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new InstalarPneuCommand(pneuId, payload.VeiculoId, payload.Eixo, payload.Lado, payload.OdometroVeiculo),
                cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        // Remove o pneu instalado (rodizio/manutencao), aferindo o sulco.
        grupo.MapPost("/pneus/{pneuId:guid}/remocao", async (
            Guid pneuId, RemoverPneuPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new RemoverPneuCommand(pneuId, payload.OdometroVeiculo, payload.SulcoAferidoMilimetros, payload.RetornarAoEstoque),
                cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        // Envia o pneu (removido) para recapagem.
        grupo.MapPost("/pneus/{pneuId:guid}/recapagem", async (
            Guid pneuId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EnviarPneuParaRecapagemCommand(pneuId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        // Conclui a recapagem (restaura o sulco, acresce o custo, devolve ao estoque).
        grupo.MapPost("/pneus/{pneuId:guid}/recapagem/conclusao", async (
            Guid pneuId, ConcluirRecapagemPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new ConcluirRecapagemCommand(pneuId, payload.SulcoRecapadoMilimetros, payload.CustoRecapagem),
                cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        // Descarta/sucateia o pneu (fim de vida util); terminal.
        grupo.MapPost("/pneus/{pneuId:guid}/descarte", async (
            Guid pneuId, MotivoPneuPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DescartarPneuCommand(pneuId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");
    }

    private static void MapearApolices(RouteGroupBuilder grupo)
    {
        // Contrata (registra) uma apolice de seguro para um veiculo.
        grupo.MapPost("/apolices", async (
            ContratarApoliceCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("patrimonio.gerenciar");

        // Renova a apolice (estende a vigencia, novo premio).
        grupo.MapPost("/apolices/{apoliceId:guid}/renovacao", async (
            Guid apoliceId, RenovarApolicePayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new RenovarApoliceCommand(apoliceId, payload.NovoInicioVigencia, payload.NovoFimVigencia, payload.NovoPremio),
                cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        // Cancela a apolice (endosso de cancelamento); terminal.
        grupo.MapPost("/apolices/{apoliceId:guid}/cancelamento", async (
            Guid apoliceId, MotivoPneuPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarApoliceCommand(apoliceId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");
    }

    private sealed record InstalarPneuPayload(Guid VeiculoId, Eixo Eixo, LadoMontagem Lado, int OdometroVeiculo);

    private sealed record RemoverPneuPayload(int OdometroVeiculo, decimal SulcoAferidoMilimetros, bool RetornarAoEstoque);

    private sealed record ConcluirRecapagemPayload(decimal SulcoRecapadoMilimetros, decimal CustoRecapagem);

    private sealed record MotivoPneuPayload(string Motivo);

    private sealed record RenovarApolicePayload(DateOnly NovoInicioVigencia, DateOnly NovoFimVigencia, decimal NovoPremio);
}
