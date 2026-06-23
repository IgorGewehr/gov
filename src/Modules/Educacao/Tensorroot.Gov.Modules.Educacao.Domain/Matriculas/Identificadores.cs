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

// Os identificadores de Aluno, Turma e Escola referenciados pela Matricula sao agora os
// identificadores CANONICOS dos respectivos agregados (Onda 1 de profundidade): a Matricula deixa
// de carregar IDs "soltos" e passa a referenciar entidades reais por Id. O re-apontamento e feito
// por alias global (ver GlobalUsings.cs deste projeto), mantendo a Matricula INALTERADA.
