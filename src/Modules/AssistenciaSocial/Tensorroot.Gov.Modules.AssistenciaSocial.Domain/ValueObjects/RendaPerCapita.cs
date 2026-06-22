using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor que representa a renda per capita familiar (renda familiar total dividida
/// pelo numero de membros). Base de elegibilidade socioassistencial. Nunca negativa. O salario
/// minimo vigente NAO e constante de dominio: e parametrizado por vigencia/competencia e
/// informado pelo chamador nas comparacoes (CLAUDE.md secao 7).
/// </summary>
public sealed class RendaPerCapita : ValueObject
{
    private RendaPerCapita(decimal valor) => Valor = valor;

    /// <summary>Renda per capita (2 casas decimais).</summary>
    public decimal Valor { get; }

    /// <summary>Calcula a renda per capita a partir da renda familiar total e do numero de membros.</summary>
    /// <param name="rendaFamiliarTotal">Soma das rendas individuais (nao-negativa).</param>
    /// <param name="quantidadeMembros">Numero de membros (maior ou igual a 1).</param>
    /// <returns>Instancia de <see cref="RendaPerCapita"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a renda for negativa ou o numero de membros for menor que 1.</exception>
    public static RendaPerCapita Calcular(decimal rendaFamiliarTotal, int quantidadeMembros)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rendaFamiliarTotal);
        // I-4: divisao por zero proibida — numero de membros sempre maior ou igual a 1.
        ArgumentOutOfRangeException.ThrowIfLessThan(quantidadeMembros, 1);
        var perCapita = decimal.Round(rendaFamiliarTotal / quantidadeMembros, 2, MidpointRounding.AwayFromZero);
        return new RendaPerCapita(perCapita);
    }

    /// <summary>Indica se a renda per capita e igual ou inferior a meio salario minimo vigente.</summary>
    /// <param name="salarioMinimoVigente">Salario minimo vigente na competencia (parametrizado).</param>
    /// <returns><c>true</c> se ate meio salario minimo.</returns>
    public bool EhAteMeioSalario(decimal salarioMinimoVigente)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(salarioMinimoVigente);
        return Valor <= salarioMinimoVigente / 2m;
    }

    /// <summary>Indica se a renda per capita e inferior a um quarto do salario minimo vigente.</summary>
    /// <param name="salarioMinimoVigente">Salario minimo vigente na competencia (parametrizado).</param>
    /// <returns><c>true</c> se abaixo de um quarto do salario minimo.</returns>
    public bool EhAbaixoUmQuarto(decimal salarioMinimoVigente)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(salarioMinimoVigente);
        return Valor < salarioMinimoVigente / 4m;
    }

    /// <inheritdoc />
    public override string ToString() => Valor.ToString("0.00", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}
