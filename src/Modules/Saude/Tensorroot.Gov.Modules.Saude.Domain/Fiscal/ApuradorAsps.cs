namespace Tensorroot.Gov.Modules.Saude.Domain.Fiscal;

/// <summary>Linha de despesa de Saúde a apurar (funcional + fonte + valor executado).</summary>
/// <param name="Codigo">Funcional resolvida (função/subfunção) da despesa.</param>
/// <param name="FonteRecurso">Fonte/destinação de recurso (PCASP), opcional.</param>
/// <param name="Valor">Valor executado da despesa (&gt;= 0).</param>
public readonly record struct DespesaSaude(CodigoFuncionalSaude Codigo, string? FonteRecurso, decimal Valor);

/// <summary>
/// <b>S-1 — ApuradorAsps.</b> Especialização (refinamento) do <c>ApuradorMinimo</c> do M7.0 para as
/// <b>Ações e Serviços Públicos de Saúde</b> (LC 141/2012). Onde o apurador genérico recebe a despesa
/// "já classificada", o ApuradorAsps faz a <b>classificação FINA</b> dela: aplica o
/// <see cref="ClassificadorAsps"/> a cada despesa de Saúde, soma <b>apenas o que computa</b> (art. 3º) e
/// descarta o que não computa (art. 4º — inativos, assistência a servidor, saneamento geral, merenda,
/// limpeza urbana...), e então apura o indicador contra o mínimo vigente (default 15%, parametrizável).
/// <para>
/// Puro e <b>reprodutível</b> (sem relógio nem estado): mesmas despesas + regras + receita-base + % ⇒
/// mesmo <see cref="IndicadorAsps"/>. As regras chegam já filtradas por vigência pelo chamador (Via A2).
/// </para>
/// // TODO(validar-oficial): a classificação fina ASPS (inclusões/exclusões da LC 141 arts. 3º/4º por
/// subfunção/fonte) segue o seed default até o <b>Manual SIOPS</b> vigente — ver <see cref="RegraClassificacaoAsps"/>.
/// </summary>
public static class ApuradorAsps
{
    /// <summary>
    /// Apura o total aplicado em ASPS a partir das despesas de Saúde e das regras vigentes (só soma o
    /// que <see cref="ClassificadorAsps.ComputaNasAsps"/> aceita).
    /// </summary>
    /// <param name="despesas">Despesas de Saúde do período (funcional/fonte/valor).</param>
    /// <param name="regras">Regras de classificação ASPS vigentes do tenant.</param>
    /// <returns>Total aplicado em ASPS (despesa computável).</returns>
    public static decimal ApurarAplicadoComputavel(
        IReadOnlyCollection<DespesaSaude> despesas,
        IReadOnlyCollection<RegraClassificacaoAsps> regras)
    {
        ArgumentNullException.ThrowIfNull(despesas);
        ArgumentNullException.ThrowIfNull(regras);

        decimal aplicado = 0m;
        foreach (var despesa in despesas)
        {
            if (ClassificadorAsps.ComputaNasAsps(despesa.Codigo, despesa.FonteRecurso, regras))
            {
                aplicado += despesa.Valor;
            }
        }

        return aplicado;
    }

    /// <summary>
    /// Apura o indicador ASPS de um período: classifica e soma a despesa computável e a confronta com a
    /// receita-base e o mínimo vigente.
    /// </summary>
    /// <param name="receitaBase">Receita-base (impostos + transferências constitucionais), &gt;= 0.</param>
    /// <param name="despesas">Despesas de Saúde do período.</param>
    /// <param name="regras">Regras de classificação ASPS vigentes do tenant.</param>
    /// <param name="percentualMinimo">Percentual mínimo vigente (0..1; default legal 15%), parametrizável.</param>
    /// <returns>Indicador ASPS apurado.</returns>
    public static IndicadorAsps Apurar(
        decimal receitaBase,
        IReadOnlyCollection<DespesaSaude> despesas,
        IReadOnlyCollection<RegraClassificacaoAsps> regras,
        decimal percentualMinimo)
    {
        var aplicado = ApurarAplicadoComputavel(despesas, regras);
        return IndicadorAsps.Apurar(receitaBase, aplicado, percentualMinimo);
    }
}
