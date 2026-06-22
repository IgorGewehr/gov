namespace Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

/// <summary>
/// Saldo de almoxarifado: quantidade total disponível de um item, nunca negativa (I-1).
/// </summary>
/// <param name="Quantidade">Quantidade disponível (maior ou igual a zero).</param>
public readonly record struct SaldoAlmoxarifado(decimal Quantidade)
{
    /// <summary>Saldo zero.</summary>
    public static SaldoAlmoxarifado Zero => new(0m);

    /// <summary>Cria um saldo não-negativo.</summary>
    /// <param name="quantidade">Quantidade (maior ou igual a zero).</param>
    /// <returns>Novo <see cref="SaldoAlmoxarifado"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade for negativa.</exception>
    public static SaldoAlmoxarifado De(decimal quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantidade);
        return new SaldoAlmoxarifado(quantidade);
    }

    /// <summary>Acrescenta uma quantidade positiva ao saldo (entrada).</summary>
    /// <param name="quantidade">Quantidade a acrescentar (estritamente positiva).</param>
    /// <returns>Novo saldo com o incremento.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade não for positiva.</exception>
    public SaldoAlmoxarifado Acrescentar(decimal quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        return new SaldoAlmoxarifado(Quantidade + quantidade);
    }

    /// <summary>Baixa uma quantidade positiva do saldo (saída), sem permitir resultado negativo (I-1).</summary>
    /// <param name="quantidade">Quantidade a baixar (estritamente positiva e menor ou igual ao saldo).</param>
    /// <returns>Novo saldo com a redução.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade não for positiva.</exception>
    /// <exception cref="InvalidOperationException">Se a quantidade exceder o saldo disponível.</exception>
    public SaldoAlmoxarifado Baixar(decimal quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        if (quantidade > Quantidade)
        {
            throw new InvalidOperationException("Saldo insuficiente para a baixa solicitada.");
        }

        return new SaldoAlmoxarifado(Quantidade - quantidade);
    }
}
