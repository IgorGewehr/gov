using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Estoque;

/// <summary>Identificador forte da entidade <see cref="Lote"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LoteId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LoteId"/>.</returns>
    public static LoteId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Lote: partida de entrada de um item, com custo unitário e validade próprios.
/// Base do custeio PEPS (ordena pela data de entrada, I-8).
/// </summary>
public sealed class Lote : Entity<LoteId>
{
    private Lote()
    {
    }

    private Lote(
        LoteId id,
        ValorMonetario custoUnitario,
        decimal quantidadeEntrada,
        DateOnly dataEntrada,
        DateOnly? validade)
        : base(id)
    {
        CustoUnitario = custoUnitario;
        QuantidadeEntrada = quantidadeEntrada;
        QuantidadeRestante = quantidadeEntrada;
        DataEntrada = dataEntrada;
        Validade = validade;
    }

    /// <summary>Custo unitário do lote (custo de ingresso).</summary>
    public ValorMonetario CustoUnitario { get; private set; } = default!;

    /// <summary>Quantidade originalmente recebida no lote.</summary>
    public decimal QuantidadeEntrada { get; private set; }

    /// <summary>Quantidade ainda disponível no lote (consumida pelo PEPS).</summary>
    public decimal QuantidadeRestante { get; private set; }

    /// <summary>Data de entrada do lote (ordena o PEPS, I-8).</summary>
    public DateOnly DataEntrada { get; private set; }

    /// <summary>Validade do lote, quando aplicável.</summary>
    public DateOnly? Validade { get; private set; }

    /// <summary>Indica se o lote ainda possui quantidade restante.</summary>
    public bool TemSaldo => QuantidadeRestante > 0m;

    /// <summary>Registra um novo lote de entrada.</summary>
    /// <param name="custoUnitario">Custo unitário do lote.</param>
    /// <param name="quantidadeEntrada">Quantidade recebida (estritamente positiva).</param>
    /// <param name="dataEntrada">Data de entrada.</param>
    /// <param name="validade">Validade opcional.</param>
    /// <returns>Novo <see cref="Lote"/>.</returns>
    /// <exception cref="ArgumentNullException">Se o custo unitário for nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade não for positiva.</exception>
    public static Lote Registrar(
        ValorMonetario custoUnitario,
        decimal quantidadeEntrada,
        DateOnly dataEntrada,
        DateOnly? validade)
    {
        ArgumentNullException.ThrowIfNull(custoUnitario);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidadeEntrada);
        return new Lote(LoteId.New(), custoUnitario, quantidadeEntrada, dataEntrada, validade);
    }

    /// <summary>Consome uma quantidade do lote pelo PEPS, limitada à quantidade restante.</summary>
    /// <param name="quantidade">Quantidade desejada (estritamente positiva).</param>
    /// <returns>A quantidade efetivamente consumida deste lote.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade não for positiva.</exception>
    public decimal Consumir(decimal quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        var consumida = Math.Min(quantidade, QuantidadeRestante);
        QuantidadeRestante -= consumida;
        return consumida;
    }
}
