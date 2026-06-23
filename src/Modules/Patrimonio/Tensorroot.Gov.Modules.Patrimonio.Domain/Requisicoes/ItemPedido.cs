using Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Requisicoes;

/// <summary>Identificador forte de um <see cref="ItemPedido"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemPedidoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemPedidoId"/>.</returns>
    public static ItemPedidoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Linha de um pedido de requisição (entidade-filha de <see cref="PedidoRequisicao"/>): aponta para
/// um <see cref="ItemEstoque"/> existente (por Id — FK lógica, reuso do motor de estoque), registra a
/// quantidade solicitada e acumula a quantidade efetivamente atendida (permite atendimento parcial).
/// </summary>
public sealed class ItemPedido : Entity<ItemPedidoId>
{
    private ItemPedido()
    {
    }

    private ItemPedido(ItemPedidoId id, ItemEstoqueId itemEstoqueId, decimal quantidadeSolicitada)
        : base(id)
    {
        ItemEstoqueId = itemEstoqueId;
        QuantidadeSolicitada = quantidadeSolicitada;
        QuantidadeAtendida = 0m;
    }

    /// <summary>Item de almoxarifado requisitado (reuso de <see cref="ItemEstoque"/> por Id).</summary>
    public ItemEstoqueId ItemEstoqueId { get; private set; }

    /// <summary>Quantidade solicitada na abertura do pedido (estritamente positiva).</summary>
    public decimal QuantidadeSolicitada { get; private set; }

    /// <summary>Quantidade efetivamente baixada do estoque no atendimento (pode ser menor que a solicitada).</summary>
    public decimal QuantidadeAtendida { get; private set; }

    /// <summary>Indica se a linha foi totalmente atendida (atendida &gt;= solicitada).</summary>
    public bool TotalmenteAtendido => QuantidadeAtendida >= QuantidadeSolicitada;

    /// <summary>Saldo ainda a atender desta linha (solicitada − atendida; nunca negativo).</summary>
    public decimal Pendente => Math.Max(0m, QuantidadeSolicitada - QuantidadeAtendida);

    /// <summary>Cria uma nova linha de pedido para um item de almoxarifado.</summary>
    /// <param name="itemEstoqueId">Item de almoxarifado requisitado.</param>
    /// <param name="quantidadeSolicitada">Quantidade pedida (estritamente positiva, I-11).</param>
    /// <returns>Nova linha de pedido.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade não for positiva ou o item for vazio.</exception>
    public static ItemPedido Criar(ItemEstoqueId itemEstoqueId, decimal quantidadeSolicitada)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidadeSolicitada);
        if (itemEstoqueId.Value == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(itemEstoqueId), "A linha do pedido exige um item de almoxarifado.");
        }

        return new ItemPedido(ItemPedidoId.New(), itemEstoqueId, quantidadeSolicitada);
    }

    /// <summary>Registra a quantidade efetivamente atendida desta linha (acumula no atendimento).</summary>
    /// <param name="quantidade">Quantidade baixada do estoque (estritamente positiva, até o pendente).</param>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade não for positiva.</exception>
    /// <exception cref="InvalidOperationException">Se a quantidade exceder o pendente da linha.</exception>
    internal void RegistrarAtendimento(decimal quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        if (quantidade > Pendente)
        {
            throw new InvalidOperationException(
                $"Quantidade atendida ({quantidade}) excede o pendente da linha ({Pendente}).");
        }

        QuantidadeAtendida += quantidade;
    }
}
