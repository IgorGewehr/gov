using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Saude.Application.Atendimento;
using Tensorroot.Gov.Modules.Saude.Application.Pacientes;
using Tensorroot.Gov.Modules.Saude.Application.Regulacao;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do modulo Saude.</summary>
internal static class SaudeEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/saude").WithTags("Saude");

        MapearPacientes(grupo);
        MapearAtendimentos(grupo);
        MapearRegulacao(grupo);
    }

    private static void MapearPacientes(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/pacientes", async (
            CadastrarPacienteCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.gerenciar");

        // LG-A3: leitura de conteudo clinico identificavel exige o verbo FINO (separado de "saude.ver").
        grupo.MapGet("/pacientes/por-cns/{cns}", async (
            string cns, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterPacientePorCnsQuery(cns), cancellationToken))).RequirePermission("saude.prontuario.ler");

        grupo.MapGet("/pacientes/{pacienteId:guid}/historico-clinico", async (
            Guid pacienteId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterHistoricoClinicoDoPacienteQuery(pacienteId), cancellationToken))).RequirePermission("saude.prontuario.ler");

        grupo.MapPut("/pacientes/{pacienteId:guid}", async (
            Guid pacienteId, AtualizarCadastroPacientePayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AtualizarCadastroPacienteCommand(pacienteId, payload.Identificacao, payload.Endereco), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/pacientes/{pacienteId:guid}/confirmacao-cadsus", async (
            Guid pacienteId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ConfirmarCadastroNoCadsusCommand(pacienteId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/pacientes/{pacienteId:guid}/condicoes", async (
            Guid pacienteId, RegistrarCondicaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarCondicaoDeSaudeCommand(pacienteId, payload.Codigo, payload.Descricao), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/pacientes/{pacienteId:guid}/alergias", async (
            Guid pacienteId, RegistrarAlergiaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarAlergiaCommand(pacienteId, payload.Substancia, payload.Gravidade), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/pacientes/{pacienteId:guid}/inativacao", async (
            Guid pacienteId, InativarPacientePayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new InativarPacienteCommand(pacienteId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");
    }

    private static void MapearAtendimentos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/atendimentos", async (
            RegistrarAtendimentoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.gerenciar");

        // LG-A3: o detalhe e o historico de atendimentos contem conteudo clinico (SOAP/CID) — verbo fino.
        grupo.MapGet("/atendimentos/{atendimentoId:guid}", async (
            Guid atendimentoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterAtendimentoPorIdQuery(atendimentoId), cancellationToken))).RequirePermission("saude.prontuario.ler");

        grupo.MapGet("/pacientes/{pacienteId:guid}/atendimentos", async (
            Guid pacienteId, DateOnly? de, DateOnly? ate, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarAtendimentosDoPacienteQuery(pacienteId, de, ate), cancellationToken))).RequirePermission("saude.prontuario.ler");

        grupo.MapPost("/atendimentos/{atendimentoId:guid}/evolucoes", async (
            Guid atendimentoId, AdicionarEvolucaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AdicionarEvolucaoSOAPCommand(
                atendimentoId, payload.Subjetivo, payload.Objetivo, payload.Avaliacao, payload.Plano, payload.Cid, payload.Ciap), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/atendimentos/{atendimentoId:guid}/assinatura", async (
            Guid atendimentoId, AssinarAtendimentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AssinarAtendimentoCommand(atendimentoId, payload.CertificadoIcpBrasil, payload.Hash), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/atendimentos/{atendimentoId:guid}/adendos", async (
            Guid atendimentoId, AdicionarAdendoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AdicionarAdendoCommand(
                atendimentoId, payload.EvolucaoReferenciadaId, payload.Texto, payload.CertificadoIcpBrasil, payload.Hash), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/atendimentos/{atendimentoId:guid}/rnds", async (
            Guid atendimentoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CompartilharAtendimentoNaRNDSCommand(atendimentoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/atendimentos/{atendimentoId:guid}/sisab", async (
            Guid atendimentoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new LancarAtendimentoNoSISABCommand(atendimentoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/atendimentos/{atendimentoId:guid}/cancelamento", async (
            Guid atendimentoId, CancelarAtendimentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarAtendimentoCommand(atendimentoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");
    }

    private static void MapearRegulacao(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/regulacao/solicitacoes", async (
            SolicitarRegulacaoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) })).RequirePermission("saude.gerenciar");

        grupo.MapGet("/regulacao/solicitacoes/{solicitacaoId:guid}", async (
            Guid solicitacaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterSolicitacaoRegulacaoPorIdQuery(solicitacaoId), cancellationToken))).RequirePermission("saude.ver");

        grupo.MapGet("/regulacao/fila", async (
            string? codigoSigtap, Prioridade? prioridade, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarFilaDeRegulacaoQuery(codigoSigtap, prioridade), cancellationToken))).RequirePermission("saude.ver");

        grupo.MapPost("/regulacao/solicitacoes/{solicitacaoId:guid}/autorizacao", async (
            Guid solicitacaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AutorizarSolicitacaoRegulacaoCommand(solicitacaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/regulacao/solicitacoes/{solicitacaoId:guid}/negativa", async (
            Guid solicitacaoId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new NegarSolicitacaoRegulacaoCommand(solicitacaoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/regulacao/solicitacoes/{solicitacaoId:guid}/devolucao", async (
            Guid solicitacaoId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DevolverSolicitacaoRegulacaoCommand(solicitacaoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/regulacao/solicitacoes/{solicitacaoId:guid}/execucao", async (
            Guid solicitacaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ExecutarSolicitacaoRegulacaoCommand(solicitacaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");

        grupo.MapPost("/regulacao/solicitacoes/{solicitacaoId:guid}/cancelamento", async (
            Guid solicitacaoId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarSolicitacaoRegulacaoCommand(solicitacaoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("saude.gerenciar");
    }

    private sealed record AtualizarCadastroPacientePayload(IdentificacaoDto Identificacao, EnderecoDto Endereco);

    private sealed record RegistrarCondicaoPayload(string Codigo, string Descricao);

    private sealed record RegistrarAlergiaPayload(string Substancia, string Gravidade);

    private sealed record InativarPacientePayload(string Motivo);

    private sealed record AdicionarEvolucaoPayload(
        string Subjetivo, string Objetivo, string Avaliacao, string Plano, string? Cid, string? Ciap);

    private sealed record AssinarAtendimentoPayload(string CertificadoIcpBrasil, string Hash);

    private sealed record AdicionarAdendoPayload(Guid EvolucaoReferenciadaId, string Texto, string CertificadoIcpBrasil, string Hash);

    private sealed record CancelarAtendimentoPayload(string Motivo);

    private sealed record MotivoPayload(string Motivo);
}
