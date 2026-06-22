using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Estoque;

/// <summary>Resumo de um movimento de estoque (entrada/saída) para leitura.</summary>
/// <param name="Id">Identificador do movimento.</param>
/// <param name="Tipo">Tipo do movimento (Entrada/Saida).</param>
/// <param name="Quantidade">Quantidade movimentada.</param>
/// <param name="ValorUnitario">Valor unitário aplicado.</param>
/// <param name="Data">Data do movimento.</param>
/// <param name="Documento">Documento de respaldo.</param>
public sealed record MovimentoResumo(
    Guid Id,
    string Tipo,
    decimal Quantidade,
    decimal ValorUnitario,
    DateOnly Data,
    string Documento);

/// <summary>Lista os movimentos de um item de estoque em um período (tenant-scoped).</summary>
/// <param name="ItemEstoqueId">Item de estoque.</param>
/// <param name="De">Data inicial (inclusive).</param>
/// <param name="Ate">Data final (inclusive).</param>
public sealed record ListarMovimentosDoItemQuery(Guid ItemEstoqueId, DateOnly De, DateOnly Ate)
    : IQuery<IReadOnlyList<MovimentoResumo>>;

/// <summary>Handler da consulta de movimentos do item.</summary>
public sealed class ListarMovimentosDoItemHandler(IItemEstoqueRepository itens)
    : IQueryHandler<ListarMovimentosDoItemQuery, IReadOnlyList<MovimentoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MovimentoResumo>> Handle(
        ListarMovimentosDoItemQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = await itens.ObterPorIdAsync(new ItemEstoqueId(request.ItemEstoqueId), cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            return [];
        }

        return item.Movimentos
            .Where(movimento => movimento.Data >= request.De && movimento.Data <= request.Ate)
            .OrderBy(movimento => movimento.Data)
            .Select(movimento => new MovimentoResumo(
                movimento.Id.Value,
                movimento.Tipo.ToString(),
                movimento.Quantidade,
                movimento.ValorUnitario.Valor,
                movimento.Data,
                movimento.Documento))
            .ToList();
    }
}
