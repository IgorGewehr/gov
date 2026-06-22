using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Pagamentos;

namespace Tensorroot.Gov.Modules.Financas.Application.Pagamentos;

/// <summary>Item de uma ordem de pagamento para leitura.</summary>
/// <param name="LiquidacaoId">Liquidação quitada.</param>
/// <param name="Valor">Valor do item.</param>
public sealed record ItemPagamentoResumo(Guid LiquidacaoId, decimal Valor);

/// <summary>Resumo de uma ordem de pagamento para leitura.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Numero">Número.</param>
/// <param name="DataPagamento">Data do pagamento.</param>
/// <param name="ContaBancaria">Conta bancária (texto).</param>
/// <param name="ValorTotal">Valor total.</param>
/// <param name="Situacao">Situação atual.</param>
/// <param name="Itens">Itens da ordem.</param>
public sealed record OrdemDePagamentoResumo(
    Guid Id,
    string Numero,
    DateOnly DataPagamento,
    string ContaBancaria,
    decimal ValorTotal,
    string Situacao,
    IReadOnlyList<ItemPagamentoResumo> Itens);

/// <summary>Obtém uma ordem de pagamento por identificador.</summary>
/// <param name="OrdemDePagamentoId">Identificador.</param>
public sealed record ObterOrdemDePagamentoQuery(Guid OrdemDePagamentoId) : IQuery<OrdemDePagamentoResumo?>;

/// <summary>Handler da consulta de ordem de pagamento.</summary>
public sealed class ObterOrdemDePagamentoHandler(IOrdemDePagamentoRepository ordens)
    : IQueryHandler<ObterOrdemDePagamentoQuery, OrdemDePagamentoResumo?>
{
    /// <inheritdoc />
    public async Task<OrdemDePagamentoResumo?> Handle(ObterOrdemDePagamentoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ordem = await ordens.ObterPorIdAsync(new OrdemDePagamentoId(request.OrdemDePagamentoId), cancellationToken).ConfigureAwait(false);
        if (ordem is null)
        {
            return null;
        }

        return new OrdemDePagamentoResumo(
            ordem.Id.Value,
            ordem.Numero,
            ordem.DataPagamento,
            ordem.ContaBancaria.ToString(),
            ordem.ValorTotal.Valor,
            ordem.Situacao.ToString(),
            ordem.Itens.Select(i => new ItemPagamentoResumo(i.LiquidacaoId.Value, i.Valor.Valor)).ToList());
    }
}
