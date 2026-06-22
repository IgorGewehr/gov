using Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

/// <summary>
/// Ato de assinatura aplicado a um documento, com nivel (<see cref="TipoAssinatura"/>), signatario
/// e carimbo de tempo confiavel (Lei 14.063/2020). Toda assinatura exige carimbo de tempo (I-8).
/// </summary>
public sealed class Assinatura : ValueObject
{
    private Assinatura(TipoAssinatura tipo, Guid signatarioId, CarimboDeTempo carimboTempo)
    {
        Tipo = tipo;
        SignatarioId = signatarioId;
        CarimboTempo = carimboTempo;
    }

    /// <summary>Nivel da assinatura aplicada.</summary>
    public TipoAssinatura Tipo { get; }

    /// <summary>Sujeito que assinou o documento.</summary>
    public Guid SignatarioId { get; }

    /// <summary>Carimbo de tempo confiavel da assinatura.</summary>
    public CarimboDeTempo CarimboTempo { get; }

    /// <summary>Cria uma assinatura validada (signatario obrigatorio e carimbo de tempo presente).</summary>
    /// <param name="tipo">Nivel da assinatura.</param>
    /// <param name="signatarioId">Sujeito que assina.</param>
    /// <param name="carimboTempo">Carimbo de tempo confiavel (obrigatorio).</param>
    /// <returns>Instancia de <see cref="Assinatura"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o signatario nao for informado.</exception>
    /// <exception cref="ArgumentNullException">Se o carimbo de tempo nao for informado (I-8).</exception>
    public static Assinatura De(TipoAssinatura tipo, Guid signatarioId, CarimboDeTempo carimboTempo)
    {
        if (signatarioId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(signatarioId), "Signatario e obrigatorio.");
        }

        ArgumentNullException.ThrowIfNull(carimboTempo);
        return new Assinatura(tipo, signatarioId, carimboTempo);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Tipo} por {SignatarioId} em {CarimboTempo.InstanteUtc:O}";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Tipo;
        yield return SignatarioId;
        yield return CarimboTempo;
    }
}
