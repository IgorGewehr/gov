namespace Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;

/// <summary>Identificador forte do agregado <see cref="DiarioClasse"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DiarioClasseId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DiarioClasseId"/>.</returns>
    public static DiarioClasseId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de um componente curricular (disciplina/unidade de ensino) da nota.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ComponenteCurricularId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ComponenteCurricularId"/>.</returns>
    public static ComponenteCurricularId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
