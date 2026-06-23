namespace Tensorroot.Gov.Modules.Educacao.Domain.Alunos;

/// <summary>
/// Identificador forte do agregado <see cref="Aluno"/>. E a identidade canonica do aluno na rede de
/// ensino; o <c>AlunoId</c> referenciado pela <c>Matricula</c> aponta para este mesmo valor (alias).
/// </summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AlunoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AlunoId"/>.</returns>
    public static AlunoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte da entidade-filha <see cref="Responsavel"/> (vinculo do aluno).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ResponsavelId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ResponsavelId"/>.</returns>
    public static ResponsavelId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
