using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Farmacia;

/// <summary>
/// Lote fisico de um medicamento em um estoque: numero do lote, validade e saldo remanescente.
/// Entidade filha do agregado <see cref="EstoqueMedicamento"/> — toda mutacao de saldo passa pela raiz.
/// A validade governa o consumo FEFO (First-Expired, First-Out) e o bloqueio de lote vencido.
/// </summary>
public sealed class LoteMedicamento : Entity<LoteMedicamentoId>
{
    private LoteMedicamento()
    {
    }

    private LoteMedicamento(
        LoteMedicamentoId id,
        string numeroLote,
        DateOnly validade,
        decimal quantidade)
        : base(id)
    {
        NumeroLote = numeroLote;
        Validade = validade;
        Saldo = quantidade;
        QuantidadeEntrada = quantidade;
    }

    /// <summary>Numero/identificacao do lote (do fabricante).</summary>
    public string NumeroLote { get; private set; } = default!;

    /// <summary>Data de validade do lote.</summary>
    public DateOnly Validade { get; private set; }

    /// <summary>Saldo atual remanescente do lote (nunca negativo).</summary>
    public decimal Saldo { get; private set; }

    /// <summary>Quantidade originalmente recebida na entrada deste lote.</summary>
    public decimal QuantidadeEntrada { get; private set; }

    /// <summary>Cria um novo lote a partir de uma entrada de estoque.</summary>
    /// <param name="numeroLote">Numero do lote.</param>
    /// <param name="validade">Data de validade.</param>
    /// <param name="quantidade">Quantidade recebida (> 0).</param>
    /// <returns>Novo <see cref="LoteMedicamento"/>.</returns>
    /// <exception cref="ArgumentException">Se o numero do lote for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade nao for positiva.</exception>
    internal static LoteMedicamento Criar(string numeroLote, DateOnly validade, decimal quantidade)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroLote);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(quantidade, 0m);
        return new LoteMedicamento(LoteMedicamentoId.New(), numeroLote.Trim(), validade, quantidade);
    }

    /// <summary>Indica se o lote esta vencido na data de referencia.</summary>
    /// <param name="referencia">Data de referencia (hoje).</param>
    /// <returns><c>true</c> se a validade for anterior a referencia.</returns>
    public bool EstaVencido(DateOnly referencia) => Validade < referencia;

    /// <summary>Reforca o saldo do lote (entrada adicional do mesmo numero de lote/validade).</summary>
    /// <param name="quantidade">Quantidade a somar (> 0).</param>
    internal void Reforcar(decimal quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(quantidade, 0m);
        Saldo += quantidade;
        QuantidadeEntrada += quantidade;
    }

    /// <summary>Consome (baixa) uma quantidade do saldo do lote.</summary>
    /// <param name="quantidade">Quantidade a consumir (> 0 e &lt;= saldo).</param>
    /// <exception cref="InvalidOperationException">Se a quantidade exceder o saldo do lote.</exception>
    internal void Consumir(decimal quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(quantidade, 0m);
        if (quantidade > Saldo)
        {
            throw new InvalidOperationException("Quantidade a consumir excede o saldo do lote.");
        }

        Saldo -= quantidade;
    }

    /// <summary>Devolve (estorna) uma quantidade ao saldo do lote.</summary>
    /// <param name="quantidade">Quantidade a devolver (> 0).</param>
    internal void Devolver(decimal quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(quantidade, 0m);
        Saldo += quantidade;
    }
}
