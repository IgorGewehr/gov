namespace Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

/// <summary>
/// Identificador forte do agregado <see cref="Turma"/>. E a identidade canonica da turma; o
/// <c>TurmaId</c> referenciado pela <c>Matricula</c> aponta para este mesmo valor (alias).
/// </summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TurmaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="TurmaId"/>.</returns>
    public static TurmaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
