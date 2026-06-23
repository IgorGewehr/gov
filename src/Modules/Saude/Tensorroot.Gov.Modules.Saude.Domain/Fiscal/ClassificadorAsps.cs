namespace Tensorroot.Gov.Modules.Saude.Domain.Fiscal;

/// <summary>
/// <b>S-1.</b> Serviço de domínio que decide se uma despesa de Saúde <b>computa nas ASPS</b> (LC
/// 141/2012), cruzando sua funcional/fonte contra as <see cref="RegraClassificacaoAsps"/> vigentes do
/// tenant. Escolhe a regra mais <b>específica</b> (subfunção &gt; fonte &gt; função) e, no empate, a de
/// vigência mais recente; o <see cref="EfeitoAsps"/> da regra vencedora decide Inclui/Exclui. Sem regra
/// aplicável a despesa NÃO computa (default conservador — evita inflar o numerador dos 15%). Stateless e
/// determinístico (reprodutível — sem relógio).
/// </summary>
public static class ClassificadorAsps
{
    /// <summary>
    /// Classifica a despesa: <c>true</c> se computa nas ASPS, <c>false</c> caso contrário (não é função
    /// 10, ou casa numa exclusão do art. 4º, ou nenhuma regra a inclui).
    /// </summary>
    /// <param name="codigo">Funcional resolvida da despesa (função/subfunção).</param>
    /// <param name="fonteRecurso">Fonte de recurso da despesa (opcional).</param>
    /// <param name="regras">Regras de classificação ASPS do tenant (já filtradas por vigência ≤ período).</param>
    /// <returns><c>true</c> se a despesa computa nas ASPS.</returns>
    public static bool ComputaNasAsps(
        CodigoFuncionalSaude codigo,
        string? fonteRecurso,
        IEnumerable<RegraClassificacaoAsps> regras)
    {
        ArgumentNullException.ThrowIfNull(codigo);
        ArgumentNullException.ThrowIfNull(regras);

        // Despesa fora da função Saúde nunca compõe ASPS.
        if (!codigo.EhFuncaoSaude)
        {
            return false;
        }

        RegraClassificacaoAsps? melhor = null;
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

        return melhor is not null && melhor.Efeito == EfeitoAsps.Inclui;
    }
}
