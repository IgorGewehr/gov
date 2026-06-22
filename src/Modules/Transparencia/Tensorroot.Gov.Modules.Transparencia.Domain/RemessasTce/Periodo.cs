using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

/// <summary>
/// Intervalo de competência/exercício da remessa ao TCE-RS (mês/bimestre/quadrimestre/ano).
/// As fronteiras do <see cref="Numero"/> dependem do <see cref="Tipo"/> (defesa em profundidade).
/// </summary>
public sealed class Periodo : ValueObject
{
    private Periodo(int exercicio, TipoPeriodo tipo, int numero)
    {
        Exercicio = exercicio;
        Tipo = tipo;
        Numero = numero;
    }

    /// <summary>Ano de exercício (>= 1900).</summary>
    public int Exercicio { get; }

    /// <summary>Tipo do período (mensal/bimestre/quadrimestre/anual).</summary>
    public TipoPeriodo Tipo { get; }

    /// <summary>Número da competência dentro do exercício (fronteiras conforme o tipo; 0 para anual).</summary>
    public int Numero { get; }

    /// <summary>Cria um período válido, respeitando as fronteiras do tipo.</summary>
    /// <param name="exercicio">Ano de exercício (>= 1900).</param>
    /// <param name="tipo">Tipo do período.</param>
    /// <param name="numero">Número da competência (1..12 mês; 1..6 bimestre; 1..3 quadrimestre; ignorado/0 anual).</param>
    /// <returns>Instância de <see cref="Periodo"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o exercício ou as fronteiras do número forem violados.</exception>
    public static Periodo De(int exercicio, TipoPeriodo tipo, int numero)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(exercicio, 1900);

        var numeroNormalizado = tipo switch
        {
            TipoPeriodo.Mensal => GarantirFaixa(numero, 1, 12, nameof(numero)),
            TipoPeriodo.Bimestre => GarantirFaixa(numero, 1, 6, nameof(numero)),
            TipoPeriodo.Quadrimestre => GarantirFaixa(numero, 1, 3, nameof(numero)),
            TipoPeriodo.Anual => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de período inválido."),
        };

        return new Periodo(exercicio, tipo, numeroNormalizado);
    }

    /// <inheritdoc />
    public override string ToString() => Tipo switch
    {
        TipoPeriodo.Mensal => string.Create(CultureInfo.InvariantCulture, $"{Exercicio:0000}-{Numero:00}"),
        TipoPeriodo.Bimestre => string.Create(CultureInfo.InvariantCulture, $"{Exercicio:0000}-B{Numero:00}"),
        TipoPeriodo.Quadrimestre => string.Create(CultureInfo.InvariantCulture, $"{Exercicio:0000}-Q{Numero:00}"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{Exercicio:0000}-A"),
    };

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Exercicio;
        yield return Tipo;
        yield return Numero;
    }

    private static int GarantirFaixa(int valor, int minimo, int maximo, string nome)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(valor, minimo, nome);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(valor, maximo, nome);
        return valor;
    }
}
