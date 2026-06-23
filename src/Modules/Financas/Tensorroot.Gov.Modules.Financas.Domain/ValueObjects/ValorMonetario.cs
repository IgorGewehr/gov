using System.Globalization;
using System.Text.Json.Serialization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

/// <summary>Valor monetário em Reais (BRL), não-negativo, arredondado a 2 casas.</summary>
public sealed class ValorMonetario : ValueObject
{
    // [JsonConstructor]: permite a reidratacao do VO pelo System.Text.Json no round-trip do
    // Outbox (eventos de dominio que carregam valores). O nome do parametro casa com a propriedade
    // Valor. Mantem o VO imutavel e o construtor privado (dominio rico preservado).
    [JsonConstructor]
    private ValorMonetario(decimal valor) => Valor = valor;

    /// <summary>Valor zero.</summary>
    public static ValorMonetario Zero { get; } = new(0m);

    /// <summary>Montante em Reais (2 casas decimais).</summary>
    public decimal Valor { get; }

    /// <summary>Cria um valor monetário não-negativo.</summary>
    /// <param name="valor">Montante (maior ou igual a zero).</param>
    /// <returns>Instância de <see cref="ValorMonetario"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor for negativo.</exception>
    public static ValorMonetario De(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        return new ValorMonetario(decimal.Round(valor, 2, MidpointRounding.AwayFromZero));
    }

    /// <summary>Soma dois valores monetários.</summary>
    /// <param name="outro">Parcela a somar.</param>
    /// <returns>Novo valor com a soma.</returns>
    public ValorMonetario Somar(ValorMonetario outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        return De(Valor + outro.Valor);
    }

    /// <summary>Subtrai um valor monetário, garantindo não-negatividade.</summary>
    /// <param name="outro">Parcela a subtrair.</param>
    /// <returns>Novo valor com a diferença.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o resultado for negativo.</exception>
    public ValorMonetario Subtrair(ValorMonetario outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        return De(Valor - outro.Valor);
    }

    /// <summary>Indica se este valor é estritamente maior que outro.</summary>
    /// <param name="outro">Valor de comparação.</param>
    /// <returns><c>true</c> se for maior.</returns>
    public bool EhMaiorQue(ValorMonetario outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        return Valor > outro.Valor;
    }

    /// <summary>Indica se este valor é menor ou igual a outro.</summary>
    /// <param name="outro">Valor de comparação.</param>
    /// <returns><c>true</c> se for menor ou igual.</returns>
    public bool EhMenorOuIgualA(ValorMonetario outro)
    {
        ArgumentNullException.ThrowIfNull(outro);
        return Valor <= outro.Valor;
    }

    /// <summary>Indica se o valor é maior que zero.</summary>
    /// <returns><c>true</c> se positivo.</returns>
    public bool EhPositivo() => Valor > 0m;

    /// <inheritdoc />
    public override string ToString() => Valor.ToString("0.00", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
