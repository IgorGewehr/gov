using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Beneficios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Prontuarios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do modulo AssistenciaSocial.</summary>
internal static class AssistenciaSocialEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/assistenciasocial").WithTags("AssistenciaSocial");

        MapearFamilias(grupo);
        MapearBeneficios(grupo);
        MapearProntuarios(grupo);
        MapearFiscal(grupo);
    }

    private static void MapearFiscal(RouteGroupBuilder grupo)
    {
        // A-1: abrir a unidade gestora do Fundo Municipal de Assistencia Social (FMAS).
        grupo.MapPost("/fiscal/fmas", async (
            AbrirFundoMunicipalAssistenciaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("assistenciasocial.gerenciar");

        // A-1: execucao segregada por bloco/piso do FMAS (painel de execucao por piso).
        grupo.MapGet("/fiscal/fmas/{fundoId:guid}/execucao", async (
            Guid fundoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterExecucaoFmasQuery(fundoId), cancellationToken)))
            .RequirePermission("assistenciasocial.ver");

        // A-1: receber parcela do FNAS num bloco/piso/fonte (Port. 1.043/2024).
        grupo.MapPost("/fiscal/fmas/{fundoId:guid}/parcelas", async (
            Guid fundoId, ParcelaFmasPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ReceberParcelaFnasCommand(fundoId, payload.Bloco, payload.Piso, payload.FonteRecurso, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        // A-1: executar despesa num bloco/piso/fonte (transposicao livre entre blocos/pisos e vedada).
        grupo.MapPost("/fiscal/fmas/{fundoId:guid}/execucoes", async (
            Guid fundoId, ParcelaFmasPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ExecutarDespesaSuasCommand(fundoId, payload.Bloco, payload.Piso, payload.FonteRecurso, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        // A-2: (re)consolida o RMA de uma unidade na competencia a partir do Prontuario SUAS (sem dupla digitacao).
        grupo.MapPost("/fiscal/rma/consolidar", async (
            ConsolidarRmaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(new ConsolidarRmaCommand(payload.UnidadeAtendimentoId, Competencia.De(payload.Ano, payload.Mes)), cancellationToken) }))
            .RequirePermission("assistenciasocial.gerenciar");

        // A-2: fecha (sela) o RMA da competencia para envio ao MDS (RMA/SAGI).
        grupo.MapPost("/fiscal/rma/{rmaId:guid}/fechamento", async (
            Guid rmaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new FecharRmaCommand(rmaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        // A-2: consulta o RMA consolidado de uma unidade numa competencia.
        grupo.MapGet("/fiscal/rma", async (
            Guid unidadeAtendimentoId, int ano, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterRmaQuery(unidadeAtendimentoId, Competencia.De(ano, mes)), cancellationToken)))
            .RequirePermission("assistenciasocial.ver");
    }

    private static void MapearFamilias(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/familias", async (
            ReferenciarFamiliaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("assistenciasocial.gerenciar");

        grupo.MapPut("/familias/{familiaId:guid}/renda", async (
            Guid familiaId, AtualizarRendaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AtualizarRendaFamiliarCommand(familiaId, payload.Membros), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        grupo.MapPost("/familias/{familiaId:guid}/vigencia-cadastral", async (
            Guid familiaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ProcessarVigenciaCadastralCommand(familiaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        grupo.MapGet("/familias", async (
            string territorio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterFamiliasDoTerritorioQuery(territorio), cancellationToken)))
            .RequirePermission("assistenciasocial.ver");

        // LG-A3: o resumo CadUnico expoe NIS/renda (dado sensivel) — verbo FINO, separado de "ver".
        grupo.MapGet("/familias/{familiaId:guid}/cadunico", async (
            Guid familiaId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterResumoCadUnicoQuery(familiaId), cancellationToken)))
            .RequirePermission("assistenciasocial.prontuario.ler");
    }

    private static void MapearBeneficios(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/beneficios/elegibilidade", async (
            AvaliarElegibilidadeBeneficioCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("assistenciasocial.gerenciar");

        grupo.MapPost("/beneficios/{beneficioId:guid}/cesta-basica", async (
            Guid beneficioId, EntregarCestaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EntregarCestaBasicaCommand(beneficioId, payload.Quantidade), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        grupo.MapGet("/familias/{familiaId:guid}/beneficios", async (
            Guid familiaId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterBeneficiosDaFamiliaQuery(familiaId), cancellationToken)))
            .RequirePermission("assistenciasocial.ver");

        grupo.MapGet("/beneficios/concessoes", async (
            int ano, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterConcessoesPorCompetenciaQuery(Competencia.De(ano, mes)), cancellationToken)))
            .RequirePermission("assistenciasocial.ver");
    }

    private static void MapearProntuarios(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/prontuarios", async (
            AbrirProntuarioCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("assistenciasocial.gerenciar");

        grupo.MapPost("/prontuarios/{prontuarioId:guid}/atendimentos", async (
            Guid prontuarioId, RegistrarAtendimentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarAtendimentoCommand(
                prontuarioId, payload.Servico, payload.DataAtendimento, payload.Descricao, payload.ProfissionalId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        grupo.MapPost("/prontuarios/{prontuarioId:guid}/encerramento", async (
            Guid prontuarioId, EncerrarPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EncerrarAcompanhamentoCommand(prontuarioId, payload.MotivoEncerramento), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        // LG-1: o usuario do acesso e SEMPRE o principal autenticado (claim 'sub'), derivado no
        // handler — nunca um valor da query string/body do cliente. So o motivo e entrada validada.
        grupo.MapPost("/prontuarios/{prontuarioId:guid}/acessos", async (
            Guid prontuarioId, RegistrarAcessoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarAcessoProntuarioCommand(prontuarioId, payload.MotivoAcesso), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("assistenciasocial.gerenciar");

        // LG-A3: conteudo sigiloso do prontuario SUAS (atendimentos, violacoes contra menor) — verbo fino.
        grupo.MapGet("/familias/{familiaId:guid}/prontuario", async (
            Guid familiaId, string motivoAcesso, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterProntuarioDaFamiliaQuery(familiaId, motivoAcesso), cancellationToken)))
            .RequirePermission("assistenciasocial.prontuario.ler");

        // LG-A3: a trilha de acesso do prontuario revela quem leu dado sigiloso — verbo fino.
        grupo.MapGet("/prontuarios/{prontuarioId:guid}/trilha-acesso", async (
            Guid prontuarioId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterTrilhaAcessoProntuarioQuery(prontuarioId), cancellationToken)))
            .RequirePermission("assistenciasocial.prontuario.ler");
    }

    private sealed record AtualizarRendaPayload(IReadOnlyList<MembroFamiliarDto> Membros);

    private sealed record EntregarCestaPayload(int Quantidade);

    private sealed record RegistrarAtendimentoPayload(
        Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios.TipoServico Servico,
        DateOnly DataAtendimento,
        string Descricao,
        Guid ProfissionalId);

    private sealed record EncerrarPayload(string MotivoEncerramento);

    // LG-1: o UsuarioId NAO faz parte do payload — e derivado do principal autenticado no handler.
    private sealed record RegistrarAcessoPayload(string MotivoAcesso);

    // A-1: parcela/execucao do FNAS num bloco/piso/fonte do FMAS.
    private sealed record ParcelaFmasPayload(
        BlocoFinanciamentoAssistencia Bloco,
        PisoAssistencia Piso,
        string FonteRecurso,
        decimal Valor);

    // A-2: consolidacao do RMA de uma unidade numa competencia.
    private sealed record ConsolidarRmaPayload(Guid UnidadeAtendimentoId, int Ano, int Mes);
}
