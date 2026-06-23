namespace Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

/// <summary>
/// Despesa setorial já classificada e agregada, insumo do <see cref="ApuradorMinimo"/>. Representa o
/// total computável (despesa que entra no mínimo) de um setor no período.
/// </summary>
/// <param name="Setor">Setor de mínimo.</param>
/// <param name="AplicadoComputavel">Total da despesa computável do setor no período.</param>
public readonly record struct DespesaSetorialApurada(SetorMinimo Setor, decimal AplicadoComputavel);

/// <summary>
/// Parâmetros de apuração de um mínimo, <b>versionados por tenant+vigência</b> (nada hardcoded —
/// CLAUDE.md §7/§16). O default legal é informado pelo chamador a partir de <c>ParametroVigente</c>:
/// Saúde 15% (LC 141/2012), Educação 25% (CF art. 212).
/// </summary>
/// <param name="Setor">Setor ao qual o percentual se aplica.</param>
/// <param name="PercentualMinimo">Percentual mínimo vigente (0..1).</param>
public readonly record struct ParametroMinimo(SetorMinimo Setor, decimal PercentualMinimo);

/// <summary>
/// <b>M7.0.3 — ApuradorMinimo.</b> Serviço de domínio base que apura, para um setor/período, o
/// indicador do mínimo constitucional: cruza a <b>receita-base</b> (impostos + transferências
/// constitucionais) com a <b>despesa computável</b> do setor e o <b>percentual mínimo vigente</b>,
/// devolvendo um <see cref="IndicadorMinimo"/> (base, aplicado, %, limite, situação).
/// <para>
/// É <b>parametrizável</b> (percentual por setor) e <b>reprodutível</b> (puro, sem relógio nem estado).
/// As especializações Saúde (ASPS 15%) e Educação (MDE 25%) são apenas este apurador com a função e o
/// percentual corretos — a "mesma coisa com nomes diferentes" (M7-DESIGN §0).
/// </para>
/// // TODO(validar-oficial): a CLASSIFICAÇÃO da despesa computável (o que entra no numerador) depende do
/// Manual SIOPS (ASPS, LC 141 arts. 3º/4º) e do mapeamento contas→campos do SIOPE (Port. Interm. 424/2016)
/// vigentes — este núcleo apura sobre a despesa JÁ classificada (via <c>FonteRecursoVinculado</c>); a
/// fidelidade fina da classificação por anexo do RREO fica para o sub-workflow setorial (S-1/E-1).
/// </summary>
public static class ApuradorMinimo
{
    /// <summary>Apura o indicador de um setor.</summary>
    /// <param name="setor">Setor de mínimo (Saúde/Educação).</param>
    /// <param name="receitaBase">Receita-base (impostos + transferências constitucionais).</param>
    /// <param name="aplicadoComputavel">Despesa computável do setor no período.</param>
    /// <param name="percentualMinimo">Percentual mínimo vigente (0..1), parametrizável.</param>
    /// <returns>Indicador apurado.</returns>
    public static IndicadorMinimo Apurar(SetorMinimo setor, decimal receitaBase, decimal aplicadoComputavel, decimal percentualMinimo)
    {
        if (setor == SetorMinimo.Nenhum)
        {
            throw new ArgumentException("Apuração exige um setor de mínimo (Saúde/Educação).", nameof(setor));
        }

        return IndicadorMinimo.Apurar(setor, receitaBase, aplicadoComputavel, percentualMinimo);
    }

    /// <summary>
    /// Apura todos os setores parametrizados de uma vez, casando cada parâmetro com a despesa computável
    /// correspondente (0 quando o setor não tem despesa apurada). Determinístico na ordem dos parâmetros.
    /// </summary>
    /// <param name="receitaBase">Receita-base comum (impostos + transferências constitucionais).</param>
    /// <param name="despesas">Despesas computáveis por setor.</param>
    /// <param name="parametros">Percentuais mínimos vigentes por setor.</param>
    /// <returns>Indicadores apurados, um por parâmetro informado.</returns>
    public static IReadOnlyList<IndicadorMinimo> ApurarTodos(
        decimal receitaBase,
        IReadOnlyCollection<DespesaSetorialApurada> despesas,
        IReadOnlyCollection<ParametroMinimo> parametros)
    {
        ArgumentNullException.ThrowIfNull(despesas);
        ArgumentNullException.ThrowIfNull(parametros);

        var resultado = new List<IndicadorMinimo>(parametros.Count);
        foreach (var parametro in parametros)
        {
            decimal aplicado = 0m;
            foreach (var despesa in despesas)
            {
                if (despesa.Setor == parametro.Setor)
                {
                    aplicado += despesa.AplicadoComputavel;
                }
            }

            resultado.Add(Apurar(parametro.Setor, receitaBase, aplicado, parametro.PercentualMinimo));
        }

        return resultado;
    }
}
