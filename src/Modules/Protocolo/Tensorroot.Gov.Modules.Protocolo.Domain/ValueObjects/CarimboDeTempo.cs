using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

/// <summary>
/// Carimbo de tempo: atestacao temporal confiavel do instante da assinatura, emitida por uma
/// autoridade de carimbo de tempo (Lei 14.063/2020). Toda assinatura valida o carrega (I-8).
/// </summary>
public sealed class CarimboDeTempo : ValueObject
{
    /// <summary>Comprimento maximo do nome da autoridade de carimbo de tempo.</summary>
    public const int ComprimentoMaximoAutoridade = 120;

    private CarimboDeTempo(DateTime instanteUtc, string autoridade)
    {
        InstanteUtc = instanteUtc;
        Autoridade = autoridade;
    }

    /// <summary>Instante (UTC) atestado pela autoridade de carimbo de tempo.</summary>
    public DateTime InstanteUtc { get; }

    /// <summary>Autoridade de carimbo de tempo que atestou o instante.</summary>
    public string Autoridade { get; }

    /// <summary>Cria um carimbo de tempo validado.</summary>
    /// <param name="instanteUtc">Instante (UTC) atestado.</param>
    /// <param name="autoridade">Autoridade emissora do carimbo.</param>
    /// <returns>Instancia de <see cref="CarimboDeTempo"/>.</returns>
    /// <exception cref="ArgumentException">Se a autoridade for vazia ou exceder o limite.</exception>
    public static CarimboDeTempo De(DateTime instanteUtc, string autoridade)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(autoridade);
        var normalizado = autoridade.Trim();
        if (normalizado.Length > ComprimentoMaximoAutoridade)
        {
            throw new ArgumentException($"Autoridade de carimbo de tempo excede {ComprimentoMaximoAutoridade} caracteres.", nameof(autoridade));
        }

        return new CarimboDeTempo(instanteUtc, normalizado);
    }

    /// <inheritdoc />
    public override string ToString() => $"{InstanteUtc:O} ({Autoridade})";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return InstanteUtc;
        yield return Autoridade;
    }
}
