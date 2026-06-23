namespace Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;

/// <summary>
/// <b>E-1.</b> Serviço de domínio que decide se uma despesa de Educação <b>computa na MDE</b> (LDB Lei
/// 9.394/1996), cruzando sua funcional/fonte contra as <see cref="RegraClassificacaoMde"/> vigentes do
/// tenant. Escolhe a regra mais <b>específica</b> (subfunção &gt; fonte &gt; função) e, no empate, a de
/// vigência mais recente; o <see cref="EfeitoMde"/> da regra vencedora decide Inclui/Exclui. Sem regra
/// aplicável a despesa NÃO computa (default conservador — evita inflar o numerador dos 25%). Stateless e
/// determinístico (reprodutível — sem relógio). Espelha o <c>ClassificadorAsps</c> da Saúde.
/// </summary>
public static class ClassificadorMde
{
    /// <summary>
    /// Classifica a despesa: <c>true</c> se computa na MDE, <c>false</c> caso contrário (não é função 12,
    /// ou casa numa exclusão do art. 71, ou nenhuma regra a inclui).
    /// </summary>
    /// <param name="codigo">Funcional resolvida da despesa (função/subfunção).</param>
    /// <param name="fonteRecurso">Fonte de recurso da despesa (opcional).</param>
    /// <param name="regras">Regras de classificação MDE do tenant (já filtradas por vigência ≤ período).</param>
    /// <returns><c>true</c> se a despesa computa na MDE.</returns>
    public static bool ComputaNaMde(
        CodigoFuncionalEducacao codigo,
        string? fonteRecurso,
        IEnumerable<RegraClassificacaoMde> regras)
    {
        ArgumentNullException.ThrowIfNull(codigo);
        ArgumentNullException.ThrowIfNull(regras);

        // Despesa fora da função Educação nunca compõe MDE.
        if (!codigo.EhFuncaoEducacao)
        {
            return false;
        }

        RegraClassificacaoMde? melhor = null;
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

        return melhor is not null && melhor.Efeito == EfeitoMde.Inclui;
    }
}
