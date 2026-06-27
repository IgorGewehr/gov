using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tensorroot.Gov.Modules.Administracao.Application.Catalogo;
using Tensorroot.Gov.Modules.Administracao.Application.Contratos;
using Tensorroot.Gov.Modules.Administracao.Application.Dispensas;
using Tensorroot.Gov.Modules.Administracao.Application.Fornecedores;
using Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;
using Tensorroot.Gov.Modules.Administracao.Application.LicitaCon;
using Tensorroot.Gov.Modules.Administracao.Application.Pca;
using Tensorroot.Gov.Modules.Administracao.Application.RegistroPrecos;
using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;
using Tensorroot.Gov.Modules.Administracao.Domain.Pca;
using Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure;

/// <summary>Endpoints HTTP (Minimal API) do modulo Administracao (Lei 14.133/2021).</summary>
/// <remarks>
/// Particionada em arquivos por subdominio para respeitar o limite de manutenibilidade (god-file &lt; 500 linhas):
/// este arquivo cobre licitacoes, fornecedores, catalogo, ARP e PCA; <c>AdministracaoEndpoints.Contratacoes.cs</c>
/// cobre dispensas (contratacao direta) e contratos.
/// </remarks>
internal static partial class AdministracaoEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/administracao").WithTags("Administracao");

        MapLicitacoes(grupo);
        MapDispensas(grupo);
        MapContratos(grupo);
        MapFornecedores(grupo);
        MapCatalogo(grupo);
        MapAtas(grupo);
        MapPca(grupo);
        MapCredenciamentos(grupo);
    }

    private static void MapCatalogo(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/catalogo/itens", async (
            CadastrarItemCatalogoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPut("/catalogo/itens/{itemId:guid}", async (
            Guid itemId, AtualizarItemCatalogoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AtualizarItemCatalogoCommand(itemId, payload.Descricao, payload.UnidadeFornecimento, payload.Classe), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/catalogo/itens/{itemId:guid}/inativar", async (
            Guid itemId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new InativarItemCatalogoCommand(itemId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/catalogo/itens/{itemId:guid}/reativar", async (
            Guid itemId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ReativarItemCatalogoCommand(itemId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapGet("/catalogo/itens", async (
            NaturezaItem? natureza, string? termo, bool? apenasAtivos, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarItensCatalogoQuery(natureza, termo, apenasAtivos ?? false), cancellationToken)))
            .RequirePermission("administracao.ver");
    }

    private static void MapAtas(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/atas", async (
            RegistrarAtaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPost("/atas/{ataId:guid}/itens", async (
            Guid ataId, RegistrarItemAtaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                itemAtaId = await sender.Send(
                    new RegistrarItemAtaCommand(ataId, payload.ItemCatalogoId, payload.FornecedorBeneficiarioId, payload.PrecoRegistrado, payload.QuantidadeRegistrada),
                    cancellationToken),
            }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPost("/atas/{ataId:guid}/participantes", async (
            Guid ataId, IncluirParticipanteAtaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                participanteId = await sender.Send(
                    new IncluirParticipanteAtaCommand(ataId, payload.CnpjOrgao, payload.NomeOrgao),
                    cancellationToken),
            }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPost("/atas/{ataId:guid}/adesoes", async (
            Guid ataId, RegistrarAdesaoPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                adesaoId = await sender.Send(
                    new RegistrarAdesaoCommand(ataId, payload.ItemAtaId, payload.CnpjOrgaoAderente, payload.NomeOrgaoAderente, payload.Quantidade),
                    cancellationToken),
            }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPost("/atas/{ataId:guid}/contratar", async (
            Guid ataId, ContratarItemAtaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ContratarItemAtaCommand(ataId, payload.ItemAtaId, payload.Quantidade), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/atas/{ataId:guid}/itens/{itemAtaId:guid}/remanejar", async (
            Guid ataId, Guid itemAtaId, RemanejarItemAtaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RemanejarItemAtaCommand(ataId, itemAtaId, payload.NovaQuantidadeRegistrada), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/atas/{ataId:guid}/prorrogar", async (
            Guid ataId, ProrrogarAtaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new ProrrogarAtaCommand(ataId, payload.NovaVigenciaFim, payload.VantajosidadeComprovada), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/atas/{ataId:guid}/cancelar", async (
            Guid ataId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new CancelarAtaCommand(ataId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapGet("/atas/{ataId:guid}", async (
            Guid ataId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterAtaPorIdQuery(ataId), cancellationToken) is { } detalhe
                ? Results.Ok(detalhe)
                : Results.NotFound())
            .RequirePermission("administracao.ver");

        grupo.MapGet("/atas", async (
            SituacaoAta? situacao, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarAtasQuery(situacao), cancellationToken)))
            .RequirePermission("administracao.ver");
    }

    private static void MapPca(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/pca", async (
            AbrirPcaCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPost("/pca/{pcaId:guid}/itens", async (
            Guid pcaId, IncluirItemPcaPayload payload, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new
            {
                itemPcaId = await sender.Send(
                    new IncluirItemPcaCommand(pcaId, payload.ItemCatalogoId, payload.Quantidade, payload.ValorEstimado, payload.TrimestreDesejado, payload.Justificativa),
                    cancellationToken),
            }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapDelete("/pca/{pcaId:guid}/itens/{itemPcaId:guid}", async (
            Guid pcaId, Guid itemPcaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RemoverItemPcaCommand(pcaId, itemPcaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/pca/{pcaId:guid}/aprovar", async (
            Guid pcaId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new AprovarPcaCommand(pcaId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/pca/{pcaId:guid}/publicar-pncp", async (
            Guid pcaId, NumeroPncpPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new PublicarPcaNoPncpCommand(pcaId, payload.NumeroPncp), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/pca/{pcaId:guid}/revisar", async (
            Guid pcaId, MotivoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new RevisarPcaCommand(pcaId, payload.Motivo), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/pca/{pcaId:guid}/itens/{itemPcaId:guid}/vincular-contratacao", async (
            Guid pcaId, Guid itemPcaId, VincularContratacaoItemPcaPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(
                new VincularContratacaoItemPcaCommand(pcaId, itemPcaId, payload.Fonte, payload.ReferenciaId, payload.Identificacao),
                cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapGet("/pca/{exercicio:int}", async (
            int exercicio, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterPcaPorExercicioQuery(exercicio), cancellationToken) is { } detalhe
                ? Results.Ok(detalhe)
                : Results.NotFound())
            .RequirePermission("administracao.ver");
    }

    private static void MapLicitacoes(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/licitacoes", async (
            AbrirLicitacaoCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPost("/licitacoes/{licitacaoId:guid}/edital-pncp", async (
            Guid licitacaoId, PublicarEditalPncpPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            // L4: o numero de controle NAO vem mais do cliente — a ACL (IPncpGateway) transmite o edital/compra e devolve.
            await sender.Send(
                new PublicarEditalNoPncpCommand(
                    licitacaoId,
                    payload.CnpjOrgao,
                    payload.CodigoUnidade,
                    payload.AnoCompra,
                    payload.NumeroCompra,
                    payload.ModalidadeId,
                    payload.ModoDisputaId,
                    payload.AmparoLegalCodigo),
                cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        // L6: gera a remessa LicitaCon 1.4 (14 CSV) do certame e devolve um ZIP — saida validada pelo
        // e-Validador do TCE-RS (IN 13/2017). A TRANSMISSAO ao Processo Eletronico (credencial) e // TODO(M10).
        grupo.MapPost("/licitacoes/{licitacaoId:guid}/remessa-licitacon", async (
            Guid licitacaoId, GerarRemessaLicitaConPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            var remessa = await sender.Send(
                new GerarRemessaLicitaConQuery(
                    licitacaoId,
                    payload.CodigoOrgao,
                    payload.NomeOrgao,
                    payload.CodigoTipoModalidade,
                    payload.TipoObjeto,
                    payload.CodigoTipoFaseAtual,
                    payload.TipoNivelJulgamento,
                    payload.NumeroProcesso,
                    payload.AnoProcesso,
                    payload.NumeroLicitacao,
                    payload.AnoLicitacao),
                cancellationToken);

            using var memoria = new MemoryStream();
            using (var zip = new System.IO.Compression.ZipArchive(memoria, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var arquivo in remessa.Arquivos)
                {
                    var entrada = zip.CreateEntry(arquivo.Nome, System.IO.Compression.CompressionLevel.Optimal);
                    await using var fluxo = entrada.Open();
                    await fluxo.WriteAsync(arquivo.Conteudo, cancellationToken);
                }
            }

            return Results.File(memoria.ToArray(), "application/zip", $"licitacon_{licitacaoId:N}.zip");
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/licitacoes/{licitacaoId:guid}/julgar", async (
            Guid licitacaoId, JulgarPropostasPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new JulgarPropostasCommand(licitacaoId, payload.PropostaVencedoraId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapPost("/licitacoes/{licitacaoId:guid}/homologar", async (
            Guid licitacaoId, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new HomologarLicitacaoCommand(licitacaoId), cancellationToken);
            return Results.NoContent();
        }).RequirePermission("administracao.gerenciar");

        grupo.MapGet("/licitacoes/{licitacaoId:guid}", async (
            Guid licitacaoId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterLicitacaoPorIdQuery(licitacaoId), cancellationToken) is { } detalhe
                ? Results.Ok(detalhe)
                : Results.NotFound())
            .RequirePermission("administracao.ver");

        grupo.MapGet("/licitacoes", async (
            SituacaoLicitacao situacao, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarLicitacoesPorSituacaoQuery(situacao), cancellationToken)))
            .RequirePermission("administracao.ver");
    }

    private static void MapFornecedores(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/fornecedores", async (
            CadastrarFornecedorCommand comando, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(new { id = await sender.Send(comando, cancellationToken) }))
            .RequirePermission("administracao.gerenciar");

        grupo.MapPost("/fornecedores/{fornecedorId:guid}/sancoes", async (
            Guid fornecedorId, AplicarSancaoPayload payload, ISender sender, CancellationToken cancellationToken) =>
        {
            var id = await sender.Send(
                new AplicarSancaoCommand(
                    fornecedorId,
                    payload.Tipo,
                    payload.DataInicio,
                    payload.DataFim,
                    payload.ProcessoAdministrativo,
                    payload.Fundamentacao,
                    payload.ValorMulta),
                cancellationToken);
            return Results.Ok(new { sancaoId = id });
        }).RequirePermission("administracao.gerenciar");

        grupo.MapGet("/fornecedores/{fornecedorId:guid}", async (
            Guid fornecedorId, ISender sender, CancellationToken cancellationToken)
            => await sender.Send(new ObterFornecedorPorIdQuery(fornecedorId), cancellationToken) is { } detalhe
                ? Results.Ok(detalhe)
                : Results.NotFound())
            .RequirePermission("administracao.ver");

        grupo.MapGet("/fornecedores/impedidos", async (
            DateOnly referencia, ISender sender, CancellationToken cancellationToken)
            => Results.Ok(await sender.Send(new ListarFornecedoresImpedidosQuery(referencia), cancellationToken)))
            .RequirePermission("administracao.ver");
    }

    private sealed record PublicarEditalPncpPayload(
        string CnpjOrgao,
        string CodigoUnidade,
        int AnoCompra,
        string NumeroCompra,
        int ModalidadeId,
        int ModoDisputaId,
        string AmparoLegalCodigo);

    private sealed record JulgarPropostasPayload(Guid PropostaVencedoraId);

    private sealed record GerarRemessaLicitaConPayload(
        int CodigoOrgao,
        string NomeOrgao,
        int CodigoTipoModalidade,
        int TipoObjeto,
        int CodigoTipoFaseAtual,
        int TipoNivelJulgamento,
        string NumeroProcesso,
        int AnoProcesso,
        int NumeroLicitacao,
        int AnoLicitacao);

    private sealed record AdicionarItemDispensaPayload(
        Guid? ItemCatalogoId,
        string Descricao,
        decimal Quantidade,
        decimal ValorUnitarioEstimado);

    private sealed record PublicarAvisoDispensaPayload(string NumeroAviso, DateTimeOffset AberturaDisputa);

    private sealed record RegistrarLanceDispensaPayload(Guid ItemId, Guid FornecedorId, decimal Valor);

    private sealed record HomologarDispensaPayload(bool VencedorHabilitado);

    private sealed record PublicarContratoPncpPayload(
        string CnpjOrgao,
        string CodigoUnidade,
        string NumeroContratoInterno,
        int AnoContrato,
        string Processo,
        string NiFornecedor,
        Application.Abstractions.TipoPessoaFornecedorPncp TipoPessoaFornecedor,
        string NomeRazaoSocialFornecedor,
        int TipoContratoId,
        int CategoriaProcessoId,
        int NumeroParcelas = 1,
        string? CnpjCompra = null,
        int AnoCompra = 0,
        int SequencialCompra = 0,
        string? NumeroControlePncpCompra = null,
        bool FrutoAdesao = false);

    private sealed record AplicarSancaoPayload(
        Domain.Fornecedores.TipoSancao Tipo,
        DateOnly DataInicio,
        DateOnly? DataFim,
        string ProcessoAdministrativo,
        string Fundamentacao,
        decimal? ValorMulta);

    private sealed record AtualizarItemCatalogoPayload(string Descricao, string UnidadeFornecimento, string? Classe);

    private sealed record RegistrarItemAtaPayload(
        Guid ItemCatalogoId,
        Guid FornecedorBeneficiarioId,
        decimal PrecoRegistrado,
        decimal QuantidadeRegistrada);

    private sealed record RegistrarAdesaoPayload(Guid ItemAtaId, string CnpjOrgaoAderente, string NomeOrgaoAderente, decimal Quantidade);

    private sealed record IncluirParticipanteAtaPayload(string CnpjOrgao, string NomeOrgao);

    private sealed record ContratarItemAtaPayload(Guid ItemAtaId, decimal Quantidade);

    private sealed record RemanejarItemAtaPayload(decimal NovaQuantidadeRegistrada);

    private sealed record ProrrogarAtaPayload(DateOnly NovaVigenciaFim, bool VantajosidadeComprovada);

    private sealed record MotivoPayload(string Motivo);

    private sealed record IncluirItemPcaPayload(
        Guid ItemCatalogoId,
        decimal Quantidade,
        decimal ValorEstimado,
        int TrimestreDesejado,
        string? Justificativa);

    private sealed record NumeroPncpPayload(string NumeroPncp);

    private sealed record VincularContratacaoItemPcaPayload(
        FonteContratacaoPca Fonte,
        Guid ReferenciaId,
        string? Identificacao);
}
