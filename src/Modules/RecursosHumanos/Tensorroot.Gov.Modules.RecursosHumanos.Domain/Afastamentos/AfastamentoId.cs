namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;

/// <summary>Identificador forte do agregado <see cref="Afastamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AfastamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AfastamentoId"/>.</returns>
    public static AfastamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
