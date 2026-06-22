namespace Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

/// <summary>
/// Ponto de pedido: saldo mínimo que dispara a reposição do item junto à Administração (I-6).
/// </summary>
/// <param name="Quantidade">Saldo-gatilho de reposição (maior ou igual a zero).</param>
public readonly record struct PontoPedido(decimal Quantidade)
{
    /// <summary>Cria um ponto de pedido não-negativo (I-10).</summary>
    /// <param name="quantidade">Saldo-gatilho (maior ou igual a zero).</param>
    /// <returns>Novo <see cref="PontoPedido"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade for negativa.</exception>
    public static PontoPedido De(decimal quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantidade);
        return new PontoPedido(quantidade);
    }

    /// <summary>Indica se o saldo informado atingiu ou ficou abaixo do ponto de pedido (I-6).</summary>
    /// <param name="saldo">Saldo a comparar.</param>
    /// <returns><c>true</c> se o saldo for menor ou igual ao ponto de pedido.</returns>
    public bool FoiAtingidoPor(SaldoAlmoxarifado saldo) => saldo.Quantidade <= Quantidade;
}
