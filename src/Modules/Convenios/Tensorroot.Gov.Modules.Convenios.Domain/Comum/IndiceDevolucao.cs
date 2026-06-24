using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Comum;

/// <summary>
/// Indice de atualizacao de valores a devolver (saldo nao aplicado, rendimentos retidos, glosas). Modela a
/// regra "Selic acumulada a partir do 30o dia" SEM constante magica: o percentual acumulado e a norma-fonte
/// chegam dos parametros do tenant (tabela mensal por exercicio — <c>IConveniosParametros</c>). O VO apenas
/// APLICA o fator ja apurado pela Infra; o dominio nao conhece "30 dias" nem "Selic" como literais — recebe-os
/// como dados (<see cref="DiaInicioContagem"/>/<see cref="PercentualAcumulado"/>). CLAUDE.md S7/S16.
/// </summary>
public sealed class IndiceDevolucao : ValueObject
{
    private IndiceDevolucao(int diaInicioContagem, decimal percentualAcumulado, string normaFonte)
    {
        DiaInicioContagem = diaInicioContagem;
        PercentualAcumulado = percentualAcumulado;
        NormaFonte = normaFonte;
    }

    /// <summary>Dia, a partir do vencimento, em que a atualizacao comeca a correr (parametro do tenant; ex.: 30).</summary>
    public int DiaInicioContagem { get; }

    /// <summary>Percentual acumulado do indice no periodo (ex.: 4.5m = 4,5%), apurado pela Infra (tabela Selic mensal).</summary>
    public decimal PercentualAcumulado { get; }

    /// <summary>Norma-fonte citavel da regra de atualizacao (auditavel pelo Tribunal de Contas).</summary>
    public string NormaFonte { get; }

    /// <summary>
    /// Cria o indice a partir dos parametros do tenant (dia de inicio, percentual acumulado, norma-fonte).
    /// </summary>
    /// <param name="diaInicioContagem">Dia, a partir do vencimento, em que a atualizacao incide (&gt;= 0).</param>
    /// <param name="percentualAcumulado">Percentual acumulado (&gt;= 0).</param>
    /// <param name="normaFonte">Norma-fonte citavel (nao vazia).</param>
    /// <returns>Novo <see cref="IndiceDevolucao"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se algum valor for negativo.</exception>
    /// <exception cref="ArgumentException">Se a norma-fonte for vazia.</exception>
    public static IndiceDevolucao Criar(int diaInicioContagem, decimal percentualAcumulado, string normaFonte)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(diaInicioContagem);
        ArgumentOutOfRangeException.ThrowIfNegative(percentualAcumulado);
        ArgumentException.ThrowIfNullOrWhiteSpace(normaFonte);
        return new IndiceDevolucao(diaInicioContagem, percentualAcumulado, normaFonte.Trim());
    }

    /// <summary>
    /// Atualiza um valor-base pelo indice acumulado: <c>base x (1 + percentual/100)</c>. A correcao so incide
    /// quando o atraso (em dias) ja superou o <see cref="DiaInicioContagem"/>; caso contrario devolve o valor
    /// nominal (sem juros).
    /// </summary>
    /// <param name="valorBase">Valor nominal a devolver.</param>
    /// <param name="diasAtraso">Dias decorridos desde o vencimento (relogio externo).</param>
    /// <returns>Valor atualizado (ou nominal, se ainda no periodo de carencia).</returns>
    public Dinheiro Atualizar(Dinheiro valorBase, int diasAtraso)
    {
        ArgumentNullException.ThrowIfNull(valorBase);
        if (diasAtraso <= DiaInicioContagem)
        {
            return valorBase;
        }

        return Dinheiro.De(valorBase.Valor * (1m + (PercentualAcumulado / 100m)));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DiaInicioContagem;
        yield return PercentualAcumulado;
        yield return NormaFonte;
    }
}
