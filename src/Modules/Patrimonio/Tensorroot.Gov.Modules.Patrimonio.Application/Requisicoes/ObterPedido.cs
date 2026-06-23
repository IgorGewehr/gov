using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Requisicoes;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Requisicoes;

/// <summary>Linha de um pedido (leitura): item de estoque × quantidade solicitada/atendida.</summary>
/// <param name="Id">Identificador da linha.</param>
/// <param name="ItemEstoqueId">Item de almoxarifado requisitado.</param>
/// <param name="QuantidadeSolicitada">Quantidade pedida.</param>
/// <param name="QuantidadeAtendida">Quantidade efetivamente baixada do estoque.</param>
/// <param name="TotalmenteAtendido">Indica se a linha foi atendida integralmente.</param>
public sealed record ItemPedidoDto(
    Guid Id,
    Guid ItemEstoqueId,
    decimal QuantidadeSolicitada,
    decimal QuantidadeAtendida,
    bool TotalmenteAtendido);

/// <summary>Detalhe de um pedido de requisição (ficha + linhas).</summary>
/// <param name="Id">Identificador do pedido.</param>
/// <param name="UnidadeId">UO consumidora.</param>
/// <param name="SetorSolicitante">Setor solicitante.</param>
/// <param name="SolicitanteId">Servidor solicitante.</param>
/// <param name="Data">Data do pedido.</param>
/// <param name="Justificativa">Justificativa (opcional).</param>
/// <param name="Situacao">Situação na máquina de estados.</param>
/// <param name="AprovadorId">Autoridade aprovadora (se aprovado).</param>
/// <param name="DataAprovacao">Data da aprovação (se aprovado).</param>
/// <param name="DataAtendimento">Data do atendimento (se atendido).</param>
/// <param name="MotivoCancelamento">Motivo do cancelamento (se cancelado).</param>
/// <param name="Itens">Linhas do pedido.</param>
public sealed record PedidoRequisicaoDetalhe(
    Guid Id,
    Guid UnidadeId,
    string SetorSolicitante,
    Guid SolicitanteId,
    DateOnly Data,
    string? Justificativa,
    string Situacao,
    Guid? AprovadorId,
    DateOnly? DataAprovacao,
    DateOnly? DataAtendimento,
    string? MotivoCancelamento,
    IReadOnlyList<ItemPedidoDto> Itens);

/// <summary>Obtém o detalhe de um pedido de requisição (tenant-scoped).</summary>
/// <param name="PedidoRequisicaoId">Pedido a consultar.</param>
public sealed record ObterPedidoQuery(Guid PedidoRequisicaoId) : IQuery<PedidoRequisicaoDetalhe>;

/// <summary>Handler da consulta de detalhe do pedido.</summary>
public sealed class ObterPedidoHandler(IPedidoRequisicaoRepository pedidos)
    : IQueryHandler<ObterPedidoQuery, PedidoRequisicaoDetalhe>
{
    /// <inheritdoc />
    public async Task<PedidoRequisicaoDetalhe> Handle(ObterPedidoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pedido = await pedidos.ObterPorIdAsync(new PedidoRequisicaoId(request.PedidoRequisicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Pedido de requisição não encontrado.");

        return new PedidoRequisicaoDetalhe(
            pedido.Id.Value,
            pedido.UnidadeId,
            pedido.SetorSolicitante,
            pedido.SolicitanteId,
            pedido.Data,
            pedido.Justificativa,
            pedido.Situacao.ToString(),
            pedido.AprovadorId,
            pedido.DataAprovacao,
            pedido.DataAtendimento,
            pedido.MotivoCancelamento,
            pedido.Itens.Select(linha => new ItemPedidoDto(
                linha.Id.Value,
                linha.ItemEstoqueId.Value,
                linha.QuantidadeSolicitada,
                linha.QuantidadeAtendida,
                linha.TotalmenteAtendido)).ToList());
    }
}
