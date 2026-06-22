using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;

/// <summary>Identificador forte da entidade <see cref="MovimentoEstoque"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MovimentoEstoqueId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MovimentoEstoqueId"/>.</returns>
    public static MovimentoEstoqueId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Movimento de estoque: entrada ou saída que altera o saldo do item, com o valor
/// unitário aplicado conforme o método de custeio (I-3/I-4/I-5).
/// </summary>
public sealed class MovimentoEstoque : Entity<MovimentoEstoqueId>
{
    private MovimentoEstoque()
    {
    }

    private MovimentoEstoque(
        MovimentoEstoqueId id,
        TipoMovimento tipo,
        decimal quantidade,
        ValorMonetario valorUnitario,
        DateOnly data,
        string documento)
        : base(id)
    {
        Tipo = tipo;
        Quantidade = quantidade;
        ValorUnitario = valorUnitario;
        Data = data;
        Documento = documento;
    }

    /// <summary>Tipo do movimento (entrada ou saída).</summary>
    public TipoMovimento Tipo { get; private set; }

    /// <summary>Quantidade movimentada (estritamente positiva, I-11).</summary>
    public decimal Quantidade { get; private set; }

    /// <summary>Valor unitário aplicado ao movimento (deriva do método de custeio).</summary>
    public ValorMonetario ValorUnitario { get; private set; } = default!;

    /// <summary>Data do movimento.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Documento de respaldo (NF/recebimento/requisição).</summary>
    public string Documento { get; private set; } = string.Empty;

    /// <summary>Valor total do movimento (quantidade × valor unitário).</summary>
    public ValorMonetario ValorTotal => ValorUnitario.Multiplicar(Quantidade);

    /// <summary>Registra uma entrada de estoque (não reconhece despesa, I-4).</summary>
    /// <param name="quantidade">Quantidade que ingressa (estritamente positiva).</param>
    /// <param name="valorUnitario">Custo unitário de entrada.</param>
    /// <param name="data">Data do movimento.</param>
    /// <param name="documento">Documento de respaldo.</param>
    /// <returns>Novo <see cref="MovimentoEstoque"/> de entrada.</returns>
    public static MovimentoEstoque Entrada(
        decimal quantidade,
        ValorMonetario valorUnitario,
        DateOnly data,
        string documento)
    {
        ArgumentNullException.ThrowIfNull(valorUnitario);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        return new MovimentoEstoque(MovimentoEstoqueId.New(), TipoMovimento.Entrada, quantidade, valorUnitario, data, documento ?? string.Empty);
    }

    /// <summary>Registra uma saída de estoque (reconhece despesa no consumo, I-4).</summary>
    /// <param name="quantidade">Quantidade consumida (estritamente positiva).</param>
    /// <param name="valorUnitario">Valor unitário valorado pelo custeio (I-3).</param>
    /// <param name="data">Data do movimento.</param>
    /// <param name="documento">Documento de respaldo.</param>
    /// <returns>Novo <see cref="MovimentoEstoque"/> de saída.</returns>
    public static MovimentoEstoque Saida(
        decimal quantidade,
        ValorMonetario valorUnitario,
        DateOnly data,
        string documento)
    {
        ArgumentNullException.ThrowIfNull(valorUnitario);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        return new MovimentoEstoque(MovimentoEstoqueId.New(), TipoMovimento.Saida, quantidade, valorUnitario, data, documento ?? string.Empty);
    }
}
