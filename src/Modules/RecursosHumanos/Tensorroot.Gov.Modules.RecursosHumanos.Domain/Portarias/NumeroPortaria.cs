using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Portarias;

/// <summary>
/// Objeto de Valor da numeracao oficial de uma portaria: sequencial reiniciado a cada exercicio
/// (ano), unico por <c>(TenantId, Exercicio, Sequencial)</c>. Formatado como <c>NNN/AAAA</c>
/// (ex.: <c>0042/2026</c>), padrao de identificacao de atos administrativos no ente publico.
/// </summary>
public sealed class NumeroPortaria : ValueObject
{
    /// <summary>Menor sequencial valido (a numeracao comeca em 1 a cada exercicio).</summary>
    public const int SequencialMinimo = 1;

    private NumeroPortaria(int exercicio, int sequencial)
    {
        Exercicio = exercicio;
        Sequencial = sequencial;
    }

    /// <summary>Exercicio (ano civil) da numeracao.</summary>
    public int Exercicio { get; }

    /// <summary>Sequencial dentro do exercicio (>= 1).</summary>
    public int Sequencial { get; }

    /// <summary>Cria uma numeracao de portaria valida.</summary>
    /// <param name="exercicio">Exercicio (ano civil) da numeracao.</param>
    /// <param name="sequencial">Sequencial dentro do exercicio (>= 1).</param>
    /// <returns>Instancia valida de <see cref="NumeroPortaria"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o sequencial for menor que 1 ou o exercicio nao for positivo.</exception>
    public static NumeroPortaria De(int exercicio, int sequencial)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exercicio);
        if (sequencial < SequencialMinimo)
        {
            throw new ArgumentOutOfRangeException(nameof(sequencial), $"Sequencial da portaria deve ser ao menos {SequencialMinimo}.");
        }

        return new NumeroPortaria(exercicio, sequencial);
    }

    /// <summary>Formatacao oficial <c>NNN/AAAA</c> com o sequencial preenchido a 4 digitos.</summary>
    public string Formatado => $"{Sequencial.ToString("D4", CultureInfo.InvariantCulture)}/{Exercicio.ToString("D4", CultureInfo.InvariantCulture)}";

    /// <inheritdoc />
    public override string ToString() => Formatado;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Exercicio;
        yield return Sequencial;
    }
}
