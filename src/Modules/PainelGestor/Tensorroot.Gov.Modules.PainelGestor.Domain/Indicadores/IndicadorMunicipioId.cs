namespace Tensorroot.Gov.Modules.PainelGestor.Domain.Indicadores;

/// <summary>Identificador forte do read model <see cref="IndicadorMunicipioSnapshot"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct IndicadorMunicipioId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="IndicadorMunicipioId"/>.</returns>
    public static IndicadorMunicipioId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
