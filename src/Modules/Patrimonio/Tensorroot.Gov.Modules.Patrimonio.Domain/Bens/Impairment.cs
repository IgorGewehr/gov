using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

/// <summary>Identificador forte de um <see cref="Impairment"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ImpairmentId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ImpairmentId"/>.</returns>
    public static ImpairmentId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Redução ao valor recuperável (impairment) reconhecida quando o valor recuperável
/// é inferior ao valor contábil, com laudo/teste de recuperabilidade de respaldo.
/// </summary>
public sealed class Impairment : Entity<ImpairmentId>
{
    private Impairment()
    {
    }

    private Impairment(ImpairmentId id, DateOnly data, decimal valorRecuperavel, decimal perdaReconhecida, string laudoUri)
        : base(id)
    {
        Data = data;
        ValorRecuperavel = valorRecuperavel;
        PerdaReconhecida = perdaReconhecida;
        LaudoUri = laudoUri;
    }

    /// <summary>Data do teste de recuperabilidade.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Valor recuperável apurado.</summary>
    public decimal ValorRecuperavel { get; private set; }

    /// <summary>Perda reconhecida (valor contábil − valor recuperável).</summary>
    public decimal PerdaReconhecida { get; private set; }

    /// <summary>Referência (URI) do laudo/teste de recuperabilidade.</summary>
    public string LaudoUri { get; private set; } = default!;

    /// <summary>Registra uma perda por impairment.</summary>
    /// <param name="data">Data do teste.</param>
    /// <param name="valorRecuperavel">Valor recuperável.</param>
    /// <param name="perdaReconhecida">Perda reconhecida.</param>
    /// <param name="laudoUri">Referência (URI) do laudo (obrigatório).</param>
    /// <returns>Novo <see cref="Impairment"/>.</returns>
    /// <exception cref="ArgumentException">Se o laudo não for informado.</exception>
    public static Impairment Registrar(DateOnly data, decimal valorRecuperavel, decimal perdaReconhecida, string laudoUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(laudoUri);
        return new Impairment(ImpairmentId.New(), data, valorRecuperavel, perdaReconhecida, laudoUri);
    }
}
