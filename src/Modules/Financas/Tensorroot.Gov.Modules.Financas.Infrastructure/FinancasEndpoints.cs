using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Commands;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Demonstracoes;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Encerramento;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Msc;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Queries;
using Tensorroot.Gov.Modules.Financas.Application.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Application.Empenhos;
using Tensorroot.Gov.Modules.Financas.Application.Fiscal;
using Tensorroot.Gov.Modules.Financas.Application.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Application.Pagamentos;
using Tensorroot.Gov.Modules.Financas.Application.RestosAPagar;
using Tensorroot.Gov.Modules.Financas.Application.Tesouraria;
using Tensorroot.Gov.Modules.Financas.Domain.Tesouraria;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure;

/// <summary>
/// Endpoints HTTP (Minimal API) do módulo Finanças — ciclo da despesa pública orçamentária
/// (Lei 4.320/64): dotação → empenho → liquidação → pagamento → restos a pagar.
/// Leituras exigem "financas.ver"; mutações exigem "financas.gerenciar".
/// </summary>
internal static class FinancasEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/financas").WithTags("Financas");

        MapearDotacoes(grupo);
        MapearEmpenhos(grupo);
        MapearLiquidacoes(grupo);
        MapearPagamentos(grupo);
        MapearRestosAPagar(grupo);
        MapearTesouraria(grupo);
        MapearContabilidade(grupo);
        MapearFiscal(grupo);
        FinancasPlanejamentoEndpoints.Map(grupo);
    }

    private static void MapearTesouraria(RouteGroupBuilder grupo)
    {
        // Abertura de conta (bancária/caixa) com saldo inicial.
        grupo.MapPost("/tesouraria/contas", async (
            AbrirContaFinanceiraCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        // Lista de contas com saldo corrente.
        grupo.MapGet("/tesouraria/contas", async (ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarContasFinanceirasQuery(), cancellationToken)))
            .RequirePermission("financas.ver");

        // Extrato de uma conta (filtro opcional por intervalo de datas).
        grupo.MapGet("/tesouraria/contas/{contaId:guid}/extrato", async (
            Guid contaId, DateOnly? de, DateOnly? ate, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ConsultarExtratoContaQuery(contaId, de, ate), cancellationToken)))
            .RequirePermission("financas.ver");

        // Recebimento (entrada).
        grupo.MapPost("/tesouraria/contas/{contaId:guid}/recebimentos", async (
            Guid contaId, MovimentoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(
                new RegistrarRecebimentoCommand(contaId, payload.Data, payload.Valor, payload.Historico, payload.Documento), cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        // Pagamento (saída de caixa-banco).
        grupo.MapPost("/tesouraria/contas/{contaId:guid}/pagamentos", async (
            Guid contaId, MovimentoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(
                new RegistrarPagamentoCaixaCommand(contaId, payload.Data, payload.Valor, payload.Historico, payload.Documento), cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        // Transferência entre contas (operação auditada, atômica).
        grupo.MapPost("/tesouraria/transferencias", async (
            TransferirEntreContasCommand comando, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(comando, cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        // Conciliação manual de um movimento (casamento com o extrato; import OFX = M10).
        grupo.MapPost("/tesouraria/contas/{contaId:guid}/movimentos/{movimentoId:guid}/conciliar", async (
            Guid contaId, Guid movimentoId, ConciliarPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ConciliarMovimentoCommand(contaId, movimentoId, payload.DataConciliacao), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        // Boletim de Caixa/Banco (fechamento diário de receita/despesa por conta).
        grupo.MapGet("/tesouraria/boletim", async (
            DateOnly data, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new GerarBoletimCaixaBancoQuery(data), cancellationToken)))
            .RequirePermission("financas.ver");
    }

    private static void MapearFiscal(RouteGroupBuilder grupo)
    {
        // Registro PARAMETRIZÁVEL da RCL apurada (LRF — denominador do limite de pessoal no Painel do
        // Gestor). Não há fonte automática de RCL no módulo hoje; o ente informa o valor apurado (RREO)
        // e o sistema publica o ReceitaCorrenteLiquidaApuradaIntegrationEvent — sem inventar valor.
        grupo.MapPost("/fiscal/rcl", async (
            RegistrarRclPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new RegistrarReceitaCorrenteLiquidaCommand(payload.Exercicio, payload.MesReferencia, payload.ValorRcl),
                cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");
    }

    private static void MapearContabilidade(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/contabilidade/plano-de-contas/semear", async (ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { criadas = await sender.Send(new SemearPlanoDeContasCommand(), cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        grupo.MapGet("/contabilidade/plano-de-contas", async (ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarPlanoDeContasQuery(), cancellationToken)))
            .RequirePermission("financas.ver");

        grupo.MapPost("/contabilidade/lancamentos", async (
            RegistrarLancamentoManualCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        grupo.MapGet("/contabilidade/balancete", async (
            int exercicio, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ConsultarBalanceteQuery(exercicio, mes), cancellationToken)))
            .RequirePermission("financas.ver");

        grupo.MapGet("/contabilidade/contas/{contaId:guid}/razao", async (
            Guid contaId, int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ConsultarRazaoQuery(contaId, exercicio), cancellationToken)))
            .RequirePermission("financas.ver");

        // Razão ANALÍTICO: extrato lançamento-a-lançamento da conta (com saldo acumulado) — TCE.
        grupo.MapGet("/contabilidade/contas/{contaId:guid}/razao-analitico", async (
            Guid contaId, int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ConsultarRazaoAnaliticoQuery(contaId, exercicio), cancellationToken)))
            .RequirePermission("financas.ver");

        // Livro DIÁRIO: lançamentos em ordem cronológica num intervalo — exigência TCE.
        grupo.MapGet("/contabilidade/diario", async (
            int exercicio, DateOnly? de, DateOnly? ate, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ConsultarDiarioQuery(exercicio, de, ate), cancellationToken)))
            .RequirePermission("financas.ver");

        MapearMscEDemonstracoes(grupo);
        MapearEncerramento(grupo);
    }

    private static void MapearEncerramento(RouteGroupBuilder grupo)
    {
        // Encerramento completo de exercício (orquestração: RAP → apuração patrimonial/orçamentária →
        // congelamento → MSC de encerramento → abertura do seguinte). Idempotente (no-op se congelado).
        // Operação contábil sensível: exige financas.gerenciar.
        grupo.MapPost("/contabilidade/encerramento/{exercicio:int}/executar", async (
            int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new EncerrarExercicioCompletoCommand(exercicio), cancellationToken)))
            .RequirePermission("financas.gerenciar");

        // Reexecução isolada da apuração patrimonial (zera classes 3/4 — mês 13).
        grupo.MapPost("/contabilidade/encerramento/{exercicio:int}/apuracao-patrimonial", async (
            int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { lancamentos = await sender.Send(new ApurarResultadoPatrimonialCommand(exercicio), cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        // Inscrição isolada de Restos a Pagar (substitui a rota legada; mantém compat em /restos-a-pagar).
        grupo.MapPost("/contabilidade/encerramento/{exercicio:int}/inscrever-rap", async (
            int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { inscritos = await sender.Send(new EncerrarExercicioCommand(exercicio), cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        // Abertura isolada do exercício seguinte (transferência do resultado — mês 0).
        grupo.MapPost("/contabilidade/encerramento/{exercicio:int}/abrir-seguinte", async (
            int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { lancamentos = await sender.Send(new AbrirExercicioSeguinteCommand(exercicio), cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        // Geração da MSC de encerramento (anual, mês 13) — insumo do rascunho da DCA.
        grupo.MapPost("/contabilidade/msc/encerramento/gerar", async (
            GerarMscEncerramentoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new GerarMscEncerramentoCommand(payload.Exercicio, payload.PoderOrgao), cancellationToken)))
            .RequirePermission("financas.gerenciar");

        // Status do encerramento de um exercício.
        grupo.MapGet("/contabilidade/encerramento/{exercicio:int}", async (
            int exercicio, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ConsultarEncerramentoQuery(exercicio), cancellationToken) is { } status
                ? Results.Ok(status)
                : Results.NotFound())
            .RequirePermission("financas.ver");
    }

    private static void MapearMscEDemonstracoes(RouteGroupBuilder grupo)
    {
        // Geração da MSC (deriva do balancete e publica o MSCGeradaIntegrationEvent via Outbox).
        grupo.MapPost("/contabilidade/msc/gerar", async (
            GerarMscPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(
                new GerarMscCommand(payload.Exercicio, payload.Mes, payload.PoderOrgao), cancellationToken)))
            .RequirePermission("financas.gerenciar");

        // Demonstrações DCASP (read-models derivados do balancete).
        grupo.MapGet("/contabilidade/demonstracoes/balanco-orcamentario", async (
            int exercicio, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new GerarBalancoOrcamentarioQuery(exercicio, mes), cancellationToken)))
            .RequirePermission("financas.ver");

        grupo.MapGet("/contabilidade/demonstracoes/balanco-financeiro", async (
            int exercicio, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new GerarBalancoFinanceiroQuery(exercicio, mes), cancellationToken)))
            .RequirePermission("financas.ver");

        grupo.MapGet("/contabilidade/demonstracoes/balanco-patrimonial", async (
            int exercicio, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new GerarBalancoPatrimonialQuery(exercicio, mes), cancellationToken)))
            .RequirePermission("financas.ver");

        grupo.MapGet("/contabilidade/demonstracoes/variacoes-patrimoniais", async (
            int exercicio, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new GerarDvpQuery(exercicio, mes), cancellationToken)))
            .RequirePermission("financas.ver");
    }

    private static void MapearDotacoes(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/dotacoes", async (CriarDotacaoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        grupo.MapPost("/dotacoes/{dotacaoId:guid}/reforcar", async (
            Guid dotacaoId, ReforcarDotacaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ReforcarDotacaoCommand(dotacaoId, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapPost("/dotacoes/{dotacaoId:guid}/anular-credito", async (
            Guid dotacaoId, AnularCreditoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AnularCreditoDotacaoCommand(dotacaoId, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapGet("/dotacoes/{dotacaoId:guid}", async (Guid dotacaoId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterDotacaoQuery(dotacaoId), cancellationToken) is { } resumo
                ? Results.Ok(resumo)
                : Results.NotFound())
            .RequirePermission("financas.ver");

        grupo.MapGet("/dotacoes", async (int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarDotacoesPorExercicioQuery(exercicio), cancellationToken)))
            .RequirePermission("financas.ver");
    }

    private static void MapearEmpenhos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/empenhos", async (EmpenharCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        grupo.MapPost("/empenhos/{empenhoId:guid}/anular", async (
            Guid empenhoId, AnularEmpenhoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AnularEmpenhoCommand(empenhoId, payload?.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapGet("/empenhos/{empenhoId:guid}", async (Guid empenhoId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterEmpenhoQuery(empenhoId), cancellationToken) is { } resumo
                ? Results.Ok(resumo)
                : Results.NotFound())
            .RequirePermission("financas.ver");

        grupo.MapGet("/dotacoes/{dotacaoId:guid}/empenhos", async (Guid dotacaoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarEmpenhosPorDotacaoQuery(dotacaoId), cancellationToken)))
            .RequirePermission("financas.ver");
    }

    private static void MapearLiquidacoes(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/liquidacoes", async (LiquidarDespesaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        grupo.MapPost("/liquidacoes/{liquidacaoId:guid}/estornar", async (
            Guid liquidacaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EstornarLiquidacaoCommand(liquidacaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapGet("/liquidacoes/{liquidacaoId:guid}", async (Guid liquidacaoId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterLiquidacaoQuery(liquidacaoId), cancellationToken) is { } resumo
                ? Results.Ok(resumo)
                : Results.NotFound())
            .RequirePermission("financas.ver");

        grupo.MapGet("/empenhos/{empenhoId:guid}/liquidacoes", async (Guid empenhoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarLiquidacoesPorEmpenhoQuery(empenhoId), cancellationToken)))
            .RequirePermission("financas.ver");
    }

    private static void MapearPagamentos(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/ordens-pagamento", async (EmitirOrdemDePagamentoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        grupo.MapPost("/ordens-pagamento/{ordemId:guid}/efetuar", async (
            Guid ordemId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EfetuarPagamentoCommand(ordemId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapPost("/ordens-pagamento/{ordemId:guid}/cancelar", async (
            Guid ordemId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarOrdemDePagamentoCommand(ordemId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapGet("/ordens-pagamento/{ordemId:guid}", async (Guid ordemId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterOrdemDePagamentoQuery(ordemId), cancellationToken) is { } resumo
                ? Results.Ok(resumo)
                : Results.NotFound())
            .RequirePermission("financas.ver");
    }

    private static void MapearRestosAPagar(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/restos-a-pagar/encerrar-exercicio", async (
            EncerrarExercicioPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { inscritos = await sender.Send(new EncerrarExercicioCommand(payload.Exercicio), cancellationToken) }))
            .RequirePermission("financas.gerenciar");

        grupo.MapPost("/restos-a-pagar/{restoId:guid}/pagar", async (
            Guid restoId, ValorPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new PagarRestoAPagarCommand(restoId, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapPost("/restos-a-pagar/{restoId:guid}/cancelar", async (
            Guid restoId, ValorPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarRestoAPagarCommand(restoId, payload.Valor), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("financas.gerenciar");

        grupo.MapGet("/restos-a-pagar", async (int exercicioInscricao, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarRestosAPagarPorExercicioQuery(exercicioInscricao), cancellationToken)))
            .RequirePermission("financas.ver");
    }

    private sealed record ReforcarDotacaoPayload(decimal Valor);

    private sealed record AnularCreditoPayload(decimal Valor);

    private sealed record AnularEmpenhoPayload(decimal? Valor);

    private sealed record EncerrarExercicioPayload(int Exercicio);

    private sealed record ValorPayload(decimal Valor);

    private sealed record GerarMscPayload(int Exercicio, int Mes, string? PoderOrgao);

    private sealed record GerarMscEncerramentoPayload(int Exercicio, string? PoderOrgao);

    private sealed record RegistrarRclPayload(int Exercicio, int MesReferencia, decimal ValorRcl);

    private sealed record MovimentoPayload(DateOnly Data, decimal Valor, string Historico, string? Documento);

    private sealed record ConciliarPayload(DateOnly DataConciliacao);
}
