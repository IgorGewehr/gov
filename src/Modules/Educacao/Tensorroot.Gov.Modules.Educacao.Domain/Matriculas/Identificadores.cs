namespace Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

/// <summary>Identificador forte do agregado <see cref="Matricula"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MatriculaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MatriculaId"/>.</returns>
    public static MatriculaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte do aluno vinculado a matricula (sujeito de dados menor — LGPD art. 14).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AlunoId(Guid Value)
{
    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte da turma de enturmacao do aluno.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TurmaId(Guid Value)
{
    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte da escola da matricula.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EscolaId(Guid Value)
{
    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
