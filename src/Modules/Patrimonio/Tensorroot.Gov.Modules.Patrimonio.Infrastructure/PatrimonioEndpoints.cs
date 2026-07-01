using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;
using Tensorroot.Gov.Modules.Patrimonio.Application.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Application.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Application.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Application.Inventarios;
using Tensorroot.Gov.Modules.Patrimonio.Application.Requisicoes;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Requisicoes;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do módulo Patrimonio.</summary>
internal static partial class PatrimonioEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/patrimonio").WithTags("Patrimonio");

        MapearBens(grupo);
        MapearFrota(grupo);
        MapearPneus(grupo);
        MapearApolices(grupo);
        MapearEstoque(grupo);
        MapearInventarios(grupo);
        MapearRequisicoes(grupo);
        MapearObras(grupo);
    }

    // REQUISICAO DE ALMOXARIFADO self-service (Onda 3b): pedido multi-item por setor/UO -> aprovacao ->
    // atendimento com SAIDA de estoque (baixa do ItemEstoque existente, respeitando saldo; parcial permitido).
    // Orquestra ItemEstoque.AtenderRequisicao por item (reuso do motor de estoque). Maquina de estados
    // Solicitado -> Aprovado -> Atendido | Cancelado.
    private static void MapearRequisicoes(RouteGroupBuilder grupo)
    {
        // Abrir pedido (Solicitado): setor/UO + linhas (item de estoque x quantidade).
        grupo.MapPost("/requisicoes", async (
            AbrirPedidoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("patrimonio.gerenciar");

        // Fila/lista paginada de pedidos por situacao/setor/UO.
        grupo.MapGet("/requisicoes", async (
            SituacaoPedido? status, string? setor, Guid? unidadeId, int? pagina, int? tamanho,
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarPedidosQuery(status, setor, unidadeId, pagina, tamanho), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapGet("/requisicoes/{pedidoId:guid}", async (
            Guid pedidoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterPedidoQuery(pedidoId), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        // Aprovacao (Solicitado -> Aprovado): autorizacao via permissao do endpoint (RBAC do setor).
        grupo.MapPost("/requisicoes/{pedidoId:guid}/aprovacao", async (
            Guid pedidoId, AprovarPedidoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AprovarPedidoCommand(pedidoId, payload.AprovadorId, payload.Data), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        // Atendimento (Aprovado -> Atendido): baixa de estoque por item, parcial permitido.
        grupo.MapPost("/requisicoes/{pedidoId:guid}/atendimento", async (
            Guid pedidoId, AtenderPedidoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AtenderPedidoCommand(pedidoId, payload.Data), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        // Cancelamento (Solicitado/Aprovado -> Cancelado): terminal sem efeito de estoque.
        grupo.MapPost("/requisicoes/{pedidoId:guid}/cancelamento", async (
            Guid pedidoId, CancelarPedidoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarPedidoCommand(pedidoId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");
    }

    private static void MapearInventarios(RouteGroupBuilder grupo)
    {
        // Inventario (Lei 4.320 art. 96): abrir -> snapshot -> contagens/sobras -> conciliacao -> encerramento.
        grupo.MapPost("/inventarios", async (
            AbrirInventarioCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("patrimonio.gerenciar");

        // Lista/busca paginada de inventarios por exercicio/setor/situacao.
        grupo.MapGet("/inventarios", async (
            int? exercicio, string? setor, SituacaoInventario? situacao, int? pagina, int? tamanho,
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarInventariosQuery(exercicio, setor, situacao, pagina, tamanho), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapGet("/inventarios/{inventarioId:guid}", async (
            Guid inventarioId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterInventarioQuery(inventarioId), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapGet("/inventarios/{inventarioId:guid}/divergencias", async (
            Guid inventarioId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarDivergenciasQuery(inventarioId), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapPost("/inventarios/{inventarioId:guid}/snapshot", async (
            Guid inventarioId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CarregarSnapshotCommand(inventarioId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/inventarios/{inventarioId:guid}/contagens", async (
            Guid inventarioId, RegistrarContagemPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarContagemCommand(
                inventarioId, payload.BemPatrimonialId, payload.SituacaoEncontrada, payload.LocalizacaoEncontrada, payload.Observacao), cancellationToken);
            return Results.Created($"/api/patrimonio/inventarios/{inventarioId}", null);
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/inventarios/{inventarioId:guid}/sobras", async (
            Guid inventarioId, RegistrarBemNaoCadastradoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarBemNaoCadastradoCommand(
                inventarioId, payload.Descricao, payload.Localizacao, payload.ValorEstimado), cancellationToken);
            return Results.Created($"/api/patrimonio/inventarios/{inventarioId}", null);
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/inventarios/{inventarioId:guid}/conciliacao", async (
            Guid inventarioId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ConciliarInventarioCommand(inventarioId), cancellationToken)))
            .RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/inventarios/{inventarioId:guid}/encerramento", async (
            Guid inventarioId, EncerrarInventarioPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new EncerrarInventarioCommand(inventarioId, payload.DataEncerramento), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/inventarios/{inventarioId:guid}/cancelamento", async (
            Guid inventarioId, CancelarInventarioPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarInventarioCommand(inventarioId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");
    }

    private static void MapearBens(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/bens", async (
            IncorporarBemCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("patrimonio.gerenciar");

        // NAVEGABILIDADE (Onda 0): lista/busca paginada de bens por descricao/tombamento, filtros tipo/situacao.
        grupo.MapGet("/bens", async (
            string? termo, TipoBem? tipo, SituacaoBemPatrimonial? situacao, int? pagina, int? tamanho,
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarBensQuery(termo, tipo, situacao, pagina, tamanho), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapGet("/bens/{bemId:guid}", async (
            Guid bemId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterBemPatrimonialQuery(bemId), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapGet("/bens/depreciaveis", async (
            int ano, int mes, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarBensDepreciaveisQuery(ano, mes), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapGet("/bens/{bemId:guid}/movimentacoes", async (
            Guid bemId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarMovimentacoesDoBemQuery(bemId), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapPost("/bens/{bemId:guid}/tombamento", async (
            Guid bemId, TombarBemPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new TombarBemCommand(bemId, payload.NumeroTombamento), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/bens/{bemId:guid}/depreciacao", async (
            Guid bemId, DepreciarBemPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DepreciarBemCommand(bemId, payload.AnoCompetencia, payload.MesCompetencia), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/bens/{bemId:guid}/reavaliacao", async (
            Guid bemId, ReavaliarBemPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ReavaliarBemCommand(bemId, payload.NovoValorJusto, payload.LaudoUri, payload.DataReavaliacao), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/bens/{bemId:guid}/impairment", async (
            Guid bemId, RegistrarImpairmentPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarImpairmentCommand(bemId, payload.ValorRecuperavel, payload.LaudoUri, payload.DataTeste), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/bens/{bemId:guid}/transferencia", async (
            Guid bemId, TransferirBemPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new TransferirBemCommand(bemId, payload.LocalizacaoDestino, payload.ResponsavelDestinoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/bens/{bemId:guid}/cessao", async (
            Guid bemId, CederBemPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CederBemCommand(bemId, payload.TerceiroId, payload.Gratuito, payload.DataInicio, payload.DataFim), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/bens/{bemId:guid}/baixa", async (
            Guid bemId, BaixarBemPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new BaixarBemCommand(bemId, payload.MotivoBaixa, payload.LaudoUri, payload.AutorizacaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/bens/{bemId:guid}/alienacao", async (
            Guid bemId, AlienarBemPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AlienarBemCommand(bemId, payload.AvaliacaoPreviaId, payload.PorLeilao, payload.ValorAlienacao), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");
    }

    private static void MapearFrota(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/veiculos", async (
            IncorporarVeiculoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("patrimonio.gerenciar");

        // NAVEGABILIDADE (Onda 0): lista/busca paginada de veiculos por descricao/placa/RENAVAM, filtro situacao.
        grupo.MapGet("/veiculos", async (
            string? termo, SituacaoBemPatrimonial? situacao, int? pagina, int? tamanho,
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarVeiculosQuery(termo, situacao, pagina, tamanho), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapGet("/veiculos/{veiculoId:guid}", async (
            Guid veiculoId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterVeiculoQuery(veiculoId), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        // Tombamento do veiculo (EmIncorporacao -> Tombado): gate que habilita as operacoes de frota.
        grupo.MapPost("/veiculos/{veiculoId:guid}/tombamento", async (
            Guid veiculoId, TombarBemPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new TombarVeiculoCommand(veiculoId, payload.NumeroTombamento), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/veiculos/{veiculoId:guid}/abastecimentos", async (
            Guid veiculoId, RegistrarAbastecimentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarAbastecimentoCommand(
                veiculoId, payload.Data, payload.Litros, payload.Valor, payload.Odometro, payload.Horimetro, payload.MotoristaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapGet("/veiculos/{veiculoId:guid}/abastecimentos", async (
            Guid veiculoId, DateOnly de, DateOnly ate, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarAbastecimentosDoVeiculoQuery(veiculoId, de, ate), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapPost("/veiculos/{veiculoId:guid}/ordens-servico", async (
            Guid veiculoId, AbrirOrdemServicoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(new AbrirOrdemServicoCommand(veiculoId, payload.Descricao, payload.CustoEstimado, payload.Odometro), cancellationToken) }))
            .RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/veiculos/{veiculoId:guid}/ordens-servico/{ordemServicoId:guid}/conclusao", async (
            Guid veiculoId, Guid ordemServicoId, ConcluirManutencaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ConcluirManutencaoCommand(veiculoId, ordemServicoId, payload.CustoRealizado, payload.DataConclusao), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/veiculos/{veiculoId:guid}/multas", async (
            Guid veiculoId, RegistrarMultaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarMultaCommand(veiculoId, payload.CodigoInfracaoCtb, payload.Valor, payload.DataInfracao, payload.MotoristaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapGet("/veiculos/multas-pendentes", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarMultasPendentesQuery(), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapPost("/veiculos/{veiculoId:guid}/licenciamentos", async (
            Guid veiculoId, RegistrarLicenciamentoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarLicenciamentoCommand(veiculoId, payload.Exercicio, payload.ValorIpva, payload.ValorTaxa, payload.Data), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapGet("/veiculos/licenciamentos-pendentes", async (
            int exercicio, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarLicenciamentosPendentesQuery(exercicio), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapPost("/veiculos/{veiculoId:guid}/motoristas", async (
            Guid veiculoId, DesignarMotoristaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DesignarMotoristaCommand(veiculoId, payload.Nome, payload.Cnh, payload.CategoriaCnh, payload.ValidadeCnh), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        MapearPainelFrota(grupo);
    }

    // PAINEL DE FROTA (Onda 3a): read models de gestão sobre o agregado Veiculo (custo/consumo por
    // veículo e período, multas, CNH e manutenções a vencer). Zero entidade nova de domínio — projeção.
    // Multas RENAINF/DETRAN-RS e IPVA/licenciamento online = M10 (depende de convênio, atrás de ACL).
    private static void MapearPainelFrota(RouteGroupBuilder grupo)
    {
        grupo.MapGet("/frota/painel", async (
            DateOnly de, DateOnly ate, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterPainelFrotaQuery(de, ate), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapGet("/frota/veiculos/{veiculoId:guid}/custos", async (
            Guid veiculoId, DateOnly de, DateOnly ate, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterCustoPorVeiculoQuery(veiculoId, de, ate), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapGet("/frota/cnh-vencendo", async (
            int? dias, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarCnhVencendoQuery(dias ?? 30), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapGet("/frota/manutencoes/abertas", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarManutencoesAbertasQuery(), cancellationToken)))
            .RequirePermission("patrimonio.ver");
    }

    private static void MapearEstoque(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/estoque/itens", async (
            CadastrarItemEstoqueCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("patrimonio.gerenciar");

        // NAVEGABILIDADE (Onda 0): lista/busca paginada de itens por codigo/descricao, filtros situacao/classe ABC.
        grupo.MapGet("/estoque/itens", async (
            string? termo, SituacaoItemEstoque? situacao, CurvaABC? classificacaoAbc, int? pagina, int? tamanho,
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new BuscarItensEstoqueQuery(termo, situacao, classificacaoAbc, pagina, tamanho), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapGet("/estoque/itens/{itemId:guid}", async (
            Guid itemId, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterItemEstoqueQuery(itemId), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapPost("/estoque/itens/{itemId:guid}/entradas", async (
            Guid itemId, RegistrarEntradaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RegistrarEntradaCommand(
                itemId, payload.Quantidade, payload.CustoUnitario, payload.DataEntrada, payload.Validade, payload.Documento), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/estoque/itens/{itemId:guid}/requisicoes", async (
            Guid itemId, AtenderRequisicaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AtenderRequisicaoCommand(
                itemId, payload.RequisicaoId, payload.SolicitanteId, payload.Quantidade, payload.Data), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/estoque/itens/{itemId:guid}/vrl", async (
            Guid itemId, AjustarVrlPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AjustarValorRealizavelLiquidoCommand(itemId, payload.ValorRealizavelLiquido, payload.Data), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/estoque/itens/{itemId:guid}/classificacao-abc", async (
            Guid itemId, ReclassificarAbcPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ReclassificarAbcCommand(itemId, payload.ClassificacaoAbc), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapPost("/estoque/itens/{itemId:guid}/inativacao", async (
            Guid itemId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new InativarItemCommand(itemId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("patrimonio.gerenciar");

        grupo.MapGet("/estoque/itens/{itemId:guid}/movimentos", async (
            Guid itemId, DateOnly de, DateOnly ate, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarMovimentosDoItemQuery(itemId, de, ate), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapGet("/estoque/reposicao", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarItensAbaixoDoPontoPedidoQuery(), cancellationToken)))
            .RequirePermission("patrimonio.ver");

        grupo.MapGet("/estoque/curva-abc", async (
            ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ObterPosicaoCurvaAbcQuery(), cancellationToken)))
            .RequirePermission("patrimonio.ver");
    }

    private sealed record TombarBemPayload(string NumeroTombamento);

    private sealed record DepreciarBemPayload(int AnoCompetencia, int MesCompetencia);

    private sealed record ReavaliarBemPayload(decimal NovoValorJusto, string LaudoUri, DateOnly DataReavaliacao);

    private sealed record RegistrarImpairmentPayload(decimal ValorRecuperavel, string LaudoUri, DateOnly DataTeste);

    private sealed record TransferirBemPayload(string LocalizacaoDestino, Guid ResponsavelDestinoId);

    private sealed record CederBemPayload(Guid TerceiroId, bool Gratuito, DateOnly DataInicio, DateOnly? DataFim);

    private sealed record BaixarBemPayload(MotivoBaixa MotivoBaixa, string LaudoUri, Guid AutorizacaoId);

    private sealed record AlienarBemPayload(Guid AvaliacaoPreviaId, bool PorLeilao, decimal ValorAlienacao);

    private sealed record RegistrarAbastecimentoPayload(
        DateOnly Data, decimal Litros, decimal Valor, int Odometro, decimal Horimetro, Guid? MotoristaId);

    private sealed record AbrirOrdemServicoPayload(string Descricao, decimal CustoEstimado, int Odometro);

    private sealed record ConcluirManutencaoPayload(decimal CustoRealizado, DateOnly DataConclusao);

    private sealed record RegistrarMultaPayload(string CodigoInfracaoCtb, decimal Valor, DateOnly DataInfracao, Guid? MotoristaId);

    private sealed record RegistrarLicenciamentoPayload(int Exercicio, decimal ValorIpva, decimal ValorTaxa, DateOnly Data);

    private sealed record DesignarMotoristaPayload(string Nome, string Cnh, string CategoriaCnh, DateOnly ValidadeCnh);

    private sealed record RegistrarEntradaPayload(
        decimal Quantidade, decimal CustoUnitario, DateOnly DataEntrada, DateOnly? Validade, string Documento);

    private sealed record AtenderRequisicaoPayload(Guid RequisicaoId, Guid SolicitanteId, decimal Quantidade, DateOnly Data);

    private sealed record AjustarVrlPayload(decimal ValorRealizavelLiquido, DateOnly Data);

    private sealed record ReclassificarAbcPayload(int ClassificacaoAbc);

    private sealed record RegistrarContagemPayload(
        Guid BemPatrimonialId, int SituacaoEncontrada, string? LocalizacaoEncontrada, string? Observacao);

    private sealed record RegistrarBemNaoCadastradoPayload(string Descricao, string Localizacao, decimal ValorEstimado);

    private sealed record EncerrarInventarioPayload(DateOnly DataEncerramento);

    private sealed record CancelarInventarioPayload(string Motivo);

    private sealed record AprovarPedidoPayload(Guid AprovadorId, DateOnly Data);

    private sealed record AtenderPedidoPayload(DateOnly Data);

    private sealed record CancelarPedidoPayload(string Motivo);

}
