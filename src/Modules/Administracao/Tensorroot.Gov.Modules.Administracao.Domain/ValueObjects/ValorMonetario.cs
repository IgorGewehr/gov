using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

/// <summary>Valor monetario em Reais (BRL), nao-negativo, arredondado a 2 casas, com aritmetica segura.</summary>
public sealed class ValorMonetario : ValueObject
{
    private ValorMonetario(decimal valor) => Valor = valor;

    /// <summary>Valor zero.</summary>
    public static ValorMonetario Zero { get; } = new(0m);

    /// <summary>Montante em Reais (2 casas decimais).</summary>
    public decimal Valor { get; }

    /// <summary>Cria um valor monetario nao-negativo.</summary>
    /// <param name="valor">Montante (maior ou igual a zero).</param>
    /// <returns>Instancia de <see cref="ValorMonetario"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor for negativo.</exception>
    public static ValorMonetario De(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        return new ValorMonetario(decimal.Round(valor, 2, MidpointRounding.AwayFromZero));
    }

    /// <summary>Soma este valor a outro.</summary>
    /// <param name="outro">Parcela a somar.</param>
    /// <returns>Novo valor com a soma.</returns>
    public ValorMonetario Somar(ValorMonetario outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        return new ValorMonetario(Valor + outro.Valor);
    }

    /// <summary>Subtrai outro valor deste, sem permitir resultado negativo.</summary>
    /// <param name="outro">Parcela a subtrair.</param>
    /// <returns>Novo valor com a diferenca (minimo zero).</returns>
    public ValorMonetario Subtrair(ValorMonetario outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        var resultado = Valor - outro.Valor;
        return new ValorMonetario(resultado < 0m ? 0m : resultado);
    }

    /// <summary>Multiplica este valor por uma quantidade nao-negativa.</summary>
    /// <param name="quantidade">Fator multiplicador (maior ou igual a zero).</param>
    /// <returns>Novo valor com o produto.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade for negativa.</exception>
    public ValorMonetario Multiplicar(decimal quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantidade);
        return new ValorMonetario(decimal.Round(Valor * quantidade, 2, MidpointRounding.AwayFromZero));
    }

    /// <summary>Indica se este valor e maior que outro.</summary>
    /// <param name="outro">Valor a comparar.</param>
    /// <returns><c>true</c> se este valor for maior.</returns>
    public bool MaiorQue(ValorMonetario outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        return Valor > outro.Valor;
    }

    /// <summary>Indica se este valor e menor que outro.</summary>
    /// <param name="outro">Valor a comparar.</param>
    /// <returns><c>true</c> se este valor for menor.</returns>
    public bool MenorQue(ValorMonetario outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        return Valor < outro.Valor;
    }

    /// <inheritdoc />
    public override string ToString() => Valor.ToString("0.00", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
