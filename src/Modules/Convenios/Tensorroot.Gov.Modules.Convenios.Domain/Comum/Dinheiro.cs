using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Comum;

/// <summary>
/// Valor monetario em reais (BRL), nao negativo, com duas casas decimais (centavos). Imutavel; usado em
/// valores globais, parcelas, contrapartida, repasses e saldos de devolucao. A tolerancia de centavos das
/// invariantes de soma (A-INV-3) vive em <see cref="ToleranciaCentavos"/>.
/// </summary>
public sealed class Dinheiro : ValueObject
{
    /// <summary>Tolerancia de fechamento de soma (1 centavo) para conferir Sigma de parcelas (A-INV-3).</summary>
    public static readonly decimal ToleranciaCentavos = 0.01m;

    private Dinheiro(decimal valor) => Valor = valor;

    /// <summary>Valor em reais (&gt;= 0), arredondado a 2 casas.</summary>
    public decimal Valor { get; }

    /// <summary>Zero monetario.</summary>
    public static Dinheiro Zero => new(0m);

    /// <summary>
    /// Cria um valor monetario nao negativo, arredondado a 2 casas (centavos, MidpointRounding.AwayFromZero).
    /// </summary>
    /// <param name="valor">Valor em reais (&gt;= 0).</param>
    /// <returns>Novo <see cref="Dinheiro"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="valor"/> for negativo.</exception>
    public static Dinheiro De(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        return new Dinheiro(Math.Round(valor, 2, MidpointRounding.AwayFromZero));
    }

    /// <summary>Soma dois valores.</summary>
    /// <param name="outro">Parcela a somar.</param>
    /// <returns>Novo valor com a soma.</returns>
    public Dinheiro Somar(Dinheiro outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        return De(Valor + outro.Valor);
    }

    /// <summary>Subtrai um valor, nunca abaixo de zero (saldo nao negativo).</summary>
    /// <param name="outro">Valor a subtrair.</param>
    /// <returns>Novo valor (saldo); zero se a subtracao for negativa.</returns>
    public Dinheiro SubtrairAteZero(Dinheiro outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        var resultado = Valor - outro.Valor;
        return De(resultado < 0 ? 0m : resultado);
    }

    /// <summary>Aplica um percentual (ex.: 20m para 20%) sobre o valor.</summary>
    /// <param name="percentual">Percentual (0..100).</param>
    /// <returns>Novo valor proporcional.</returns>
    public Dinheiro AplicarPercentual(decimal percentual)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(percentual);
        return De(Valor * percentual / 100m);
    }

    /// <summary>Verdadeiro se este valor e maior ou igual a <paramref name="outro"/> (com tolerancia de centavos).</summary>
    /// <param name="outro">Valor de comparacao.</param>
    /// <returns><c>true</c> se &gt;= (descontada a tolerancia).</returns>
    public bool MaiorOuIgualA(Dinheiro outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        return Valor + ToleranciaCentavos >= outro.Valor;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }

    /// <inheritdoc />
    public override string ToString() => Valor.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
}
