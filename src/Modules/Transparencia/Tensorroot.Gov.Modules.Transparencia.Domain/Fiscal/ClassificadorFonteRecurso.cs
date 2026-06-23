namespace Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

/// <summary>Resultado da classificação setorial de uma despesa.</summary>
/// <param name="Setor">Setor de mínimo (ou <see cref="SetorMinimo.Nenhum"/> se não vinculado).</param>
/// <param name="ComputaNoMinimo">Se a despesa computa no mínimo do setor.</param>
public readonly record struct ClassificacaoSetorial(SetorMinimo Setor, bool ComputaNoMinimo)
{
    /// <summary>Classificação neutra (não vinculada a mínimo).</summary>
    public static ClassificacaoSetorial Nenhum { get; } = new(SetorMinimo.Nenhum, false);
}

/// <summary>
/// <b>M7.0.0.</b> Serviço de domínio que classifica uma despesa por <b>função/fonte</b> contra o
/// conjunto de regras <see cref="FonteRecursoVinculado"/> vigentes do tenant, escolhendo a regra mais
/// <b>específica</b> (a que refina por fonte vence a que vale para toda a função) e, no empate, a de
/// vigência mais recente. Stateless e determinístico (reprodutível — sem relógio).
/// </summary>
public static class ClassificadorFonteRecurso
{
    /// <summary>Classifica a despesa pela função/fonte usando as regras vigentes informadas.</summary>
    /// <param name="codigo">Funcional resolvida da despesa (função/subfunção).</param>
    /// <param name="fonteRecurso">Fonte de recurso da despesa (opcional).</param>
    /// <param name="regras">Regras de classificação do tenant (já filtradas por vigência ≤ período).</param>
    /// <returns>A classificação setorial resultante (ou <see cref="ClassificacaoSetorial.Nenhum"/>).</returns>
    public static ClassificacaoSetorial Classificar(
        CodigoFuncional codigo,
        string? fonteRecurso,
        IEnumerable<FonteRecursoVinculado> regras)
    {
        ArgumentNullException.ThrowIfNull(codigo);
        ArgumentNullException.ThrowIfNull(regras);

        FonteRecursoVinculado? melhor = null;
        foreach (var regra in regras)
        {
            if (!regra.Casa(codigo, fonteRecurso))
            {
                continue;
            }

            if (melhor is null
                || regra.Especificidade() > melhor.Especificidade()
                || (regra.Especificidade() == melhor.Especificidade() && regra.VigenciaInicio > melhor.VigenciaInicio))
            {
                melhor = regra;
            }
        }

        return melhor is null
            ? ClassificacaoSetorial.Nenhum
            : new ClassificacaoSetorial(melhor.Setor, melhor.ComputaNoMinimo);
    }
}
