using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Comum;

/// <summary>
/// Contrapartida do convenio recebido (fluxo A): aporte (financeiro ou em bens/servicos) do municipio
/// convenente. O <see cref="PercentualMinimo"/> e a <see cref="NormaFontePercentual"/> chegam dos parametros
/// do tenant (a regra dos 20% NAO e constante — Portaria Conjunta 33/2023; CLAUDE.md S7/S16). O
/// <see cref="ValorEmpenhado"/> e o espelho do empenho reconhecido via evento de Financas (A-INV-5).
/// </summary>
public sealed class Contrapartida : ValueObject
{
    private Contrapartida(
        ModalidadeContrapartida modalidade,
        Dinheiro valorPactuado,
        decimal percentualMinimo,
        string normaFontePercentual,
        Dinheiro valorEmpenhado)
    {
        Modalidade = modalidade;
        ValorPactuado = valorPactuado;
        PercentualMinimo = percentualMinimo;
        NormaFontePercentual = normaFontePercentual;
        ValorEmpenhado = valorEmpenhado;
    }

    /// <summary>Modalidade (financeira/bens-servicos).</summary>
    public ModalidadeContrapartida Modalidade { get; }

    /// <summary>Valor pactuado da contrapartida.</summary>
    public Dinheiro ValorPactuado { get; }

    /// <summary>Percentual minimo exigido (parametro do tenant; ex.: 20m). Sem numero magico.</summary>
    public decimal PercentualMinimo { get; }

    /// <summary>Norma-fonte do percentual minimo (citavel — auditoria).</summary>
    public string NormaFontePercentual { get; }

    /// <summary>Valor ja empenhado (espelho do empenho de Financas). Zero ate o reconhecimento.</summary>
    public Dinheiro ValorEmpenhado { get; }

    /// <summary>
    /// Cria a contrapartida a partir do valor pactuado e do parametro de percentual minimo do tenant.
    /// </summary>
    /// <param name="modalidade">Modalidade.</param>
    /// <param name="valorPactuado">Valor pactuado.</param>
    /// <param name="percentualMinimo">Percentual minimo exigido (parametro do tenant).</param>
    /// <param name="normaFontePercentual">Norma-fonte do percentual minimo (nao vazia).</param>
    /// <returns>Nova <see cref="Contrapartida"/> com <see cref="ValorEmpenhado"/> zerado.</returns>
    /// <exception cref="ArgumentException">Se a norma-fonte for vazia.</exception>
    public static Contrapartida Criar(
        ModalidadeContrapartida modalidade,
        Dinheiro valorPactuado,
        decimal percentualMinimo,
        string normaFontePercentual)
    {
        ArgumentNullException.ThrowIfNull(valorPactuado);
        ArgumentOutOfRangeException.ThrowIfNegative(percentualMinimo);
        ArgumentException.ThrowIfNullOrWhiteSpace(normaFontePercentual);
        return new Contrapartida(modalidade, valorPactuado, percentualMinimo, normaFontePercentual.Trim(), Dinheiro.Zero);
    }

    /// <summary>Reidrata a contrapartida a partir de valores persistidos.</summary>
    /// <param name="modalidade">Modalidade.</param>
    /// <param name="valorPactuado">Valor pactuado.</param>
    /// <param name="percentualMinimo">Percentual minimo.</param>
    /// <param name="normaFontePercentual">Norma-fonte.</param>
    /// <param name="valorEmpenhado">Valor empenhado persistido.</param>
    /// <returns>Contrapartida reconstruida.</returns>
    public static Contrapartida Reidratar(
        ModalidadeContrapartida modalidade,
        Dinheiro valorPactuado,
        decimal percentualMinimo,
        string normaFontePercentual,
        Dinheiro valorEmpenhado)
        => new(modalidade, valorPactuado, percentualMinimo, normaFontePercentual, valorEmpenhado);

    /// <summary>
    /// Verdadeiro se o valor pactuado atende ao minimo legal sobre o valor de repasse total (A-INV-2):
    /// <c>ValorPactuado &gt;= PercentualMinimo x valorRepasseTotal</c>.
    /// </summary>
    /// <param name="valorRepasseTotal">Valor total do repasse (base do percentual).</param>
    /// <returns><c>true</c> se a contrapartida atinge o minimo.</returns>
    public bool AtendeMinimo(Dinheiro valorRepasseTotal)
    {
        ArgumentNullException.ThrowIfNull(valorRepasseTotal);
        var minimo = valorRepasseTotal.AplicarPercentual(PercentualMinimo);
        return ValorPactuado.MaiorOuIgualA(minimo);
    }

    /// <summary>Verdadeiro se a contrapartida ja foi suficientemente empenhada (A-INV-5).</summary>
    public bool TotalmenteEmpenhada => ValorEmpenhado.MaiorOuIgualA(ValorPactuado);

    /// <summary>Acumula um empenho ao espelho de <see cref="ValorEmpenhado"/> (devolve nova contrapartida).</summary>
    /// <param name="valor">Valor empenhado a acumular.</param>
    /// <returns>Nova contrapartida com o empenho somado.</returns>
    public Contrapartida ComEmpenho(Dinheiro valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        return new Contrapartida(Modalidade, ValorPactuado, PercentualMinimo, NormaFontePercentual, ValorEmpenhado.Somar(valor));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Modalidade;
        yield return ValorPactuado;
        yield return PercentualMinimo;
        yield return NormaFontePercentual;
        yield return ValorEmpenhado;
    }
}
