namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

/// <summary>Identificador forte da <see cref="TabelaInss"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TabelaInssId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="TabelaInssId"/>.</returns>
    public static TabelaInssId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
