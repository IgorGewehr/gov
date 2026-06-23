using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Saude.Application.Farmacia;
using Tensorroot.Gov.Modules.Saude.Application.Imunizacao;
using Tensorroot.Gov.Modules.Saude.Domain.Imunizacao;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure;

/// <summary>
/// Endpoints HTTP da Saude operacional (Onda 3c): Farmacia/Dispensacao e Imunizacao. Particionado do
/// <see cref="SaudeEndpoints"/> principal para manutenibilidade (god-file &lt; 500 linhas).
/// </summary>
internal static partial class SaudeEndpoints
{
    private static void MapearFarmacia(RouteGroupBuilder grupo)
    {
        // Catalogo de medicamentos (REMUME).
        grupo.MapPost("/farmacia/medicamentos", async (
            CadastrarMedicamentoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.farmacia.gerenciar");

        grupo.MapGet("/farmacia/medicamentos", async (
            string? termo, bool? apenasAtivos, int? pagina, int? tamanho, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarMedicamentosQuery(termo, apenasAtivos ?? true, pagina, tamanho), cancellationToken)))
            .RequirePermission("saude.farmacia.ver");

        // Estoque por estabelecimento+medicamento (entrada de lote com validade; posicao; alertas).
        grupo.MapPost("/farmacia/estoque/{estabId:guid}/{medId:guid}/entradas", async (
            Guid estabId, Guid medId, EntradaEstoquePayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(
                new RegistrarEntradaMedicamentoCommand(estabId, medId, payload.NumeroLote, payload.Validade, payload.Quantidade, payload.PontoDeRessuprimento), cancellationToken) }))
            .RequirePermission("saude.farmacia.gerenciar");

        grupo.MapGet("/farmacia/estoque", async (
            Guid estabId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterPosicaoEstoqueQuery(estabId), cancellationToken)))
            .RequirePermission("saude.farmacia.ver");

        grupo.MapGet("/farmacia/alertas/validade", async (
            int? dias, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarAlertasValidadeQuery(dias), cancellationToken)))
            .RequirePermission("saude.farmacia.ver");

        // Dispensacao ao paciente (LGPD — vincula paciente, baixa FEFO, opcional prescricao do PEP).
        grupo.MapPost("/farmacia/dispensacoes", async (
            DispensarMedicamentoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.farmacia.dispensar");

        grupo.MapPost("/farmacia/dispensacoes/{dispensacaoId:guid}/estorno", async (
            Guid dispensacaoId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EstornarDispensacaoCommand(dispensacaoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.farmacia.dispensar");

        // Historico de dispensacao por paciente — conteudo SENSIVEL (LGPD): verbo fino + trilha de acesso.
        grupo.MapGet("/farmacia/pacientes/{pacienteId:guid}/dispensacoes", async (
            Guid pacienteId, DateOnly? de, DateOnly? ate, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarDispensacoesDoPacienteQuery(pacienteId, de, ate), cancellationToken)))
            .RequirePermission("saude.prontuario.ler");
    }

    private static void MapearImunizacao(RouteGroupBuilder grupo)
    {
        // Catalogo de imunobiologicos (PNI).
        grupo.MapPost("/imunizacao/imunobiologicos", async (
            CadastrarImunobiologicoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.imunizacao.gerenciar");

        grupo.MapGet("/imunizacao/imunobiologicos", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarImunobiologicosQuery(), cancellationToken)))
            .RequirePermission("saude.imunizacao.ver");

        // Carteira de vacinacao do paciente — conteudo SENSIVEL (LGPD): verbo fino + trilha de acesso.
        grupo.MapGet("/imunizacao/pacientes/{pacienteId:guid}/carteira", async (
            Guid pacienteId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterCarteiraVacinacaoQuery(pacienteId), cancellationToken)))
            .RequirePermission("saude.prontuario.ler");

        // Registro de aplicacao de dose (calcula aprazamento; opcional baixa de estoque do imunobiologico).
        grupo.MapPost("/imunizacao/pacientes/{pacienteId:guid}/doses", async (
            Guid pacienteId, AplicarDosePayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(
                new RegistrarDoseCommand(pacienteId, payload.ImunobiologicoId, payload.TipoDose, payload.NumeroDose, payload.Lote, payload.AplicadorId, payload.DataAplicacao, payload.EstabelecimentoId), cancellationToken) }))
            .RequirePermission("saude.imunizacao.aplicar");

        // Busca ativa: aprazamentos vencidos — dado de saude do paciente (LGPD): trilha de acesso.
        grupo.MapGet("/imunizacao/aprazamentos/vencidos", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarAprazamentosVencidosQuery(), cancellationToken)))
            .RequirePermission("saude.prontuario.ler");
    }

    private sealed record EntradaEstoquePayload(string NumeroLote, DateOnly Validade, decimal Quantidade, int PontoDeRessuprimento);

    private sealed record AplicarDosePayload(
        Guid ImunobiologicoId,
        TipoDose TipoDose,
        int NumeroDose,
        string Lote,
        Guid AplicadorId,
        DateOnly DataAplicacao,
        Guid? EstabelecimentoId);
}
