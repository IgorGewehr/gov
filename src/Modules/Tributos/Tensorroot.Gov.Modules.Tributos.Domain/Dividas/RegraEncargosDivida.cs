using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

/// <summary>
/// Encargos apurados sobre o valor originário de um crédito inscrito em Dívida Ativa:
/// correção monetária + multa de mora + juros de mora. Resultado determinístico (mesma data-base →
/// mesmo valor SEMPRE — CLAUDE.md §16).
/// </summary>
/// <param name="ValorOriginario">Valor originário inscrito (R$).</param>
/// <param name="CorrecaoMonetaria">Correção monetária apurada (R$).</param>
/// <param name="Multa">Multa de mora apurada (R$).</param>
/// <param name="Juros">Juros de mora apurados (R$).</param>
public sealed record EncargosApurados(
    ValorMonetario ValorOriginario,
    ValorMonetario CorrecaoMonetaria,
    ValorMonetario Multa,
    ValorMonetario Juros)
{
    /// <summary>Valor atualizado total = originário + correção + multa + juros (R$).</summary>
    public ValorMonetario ValorAtualizado => ValorOriginario
        .Somar(CorrecaoMonetaria)
        .Somar(Multa)
        .Somar(Juros);
}

/// <summary>
/// Regra de encargos da Dívida Ativa (multa/juros/correção), PARAMETRIZÁVEL por tenant — lei municipal
/// (CTM/REFIS). Nenhum percentual é hardcoded. A apuração usa SEMPRE datas do fato (vencimento e
/// data-base de cálculo), NUNCA o relógio do servidor: reprodutibilidade inegociável (CLAUDE.md §16).
/// <para>
/// Modelo adotado (LEF art. 2º §5º II/IV): multa de mora (percentual único sobre o originário), juros
/// de mora simples (percentual ao mês × meses em atraso) e correção monetária (percentual ao mês ×
/// meses). // TODO(validar-oficial): índice de correção (ex.: IPCA/SELIC/UFM) e capitalização conforme
/// o CTM de Maximiliano de Almeida/RS — aqui é taxa parametrizável simples; o índice oficial substitui.
/// </para>
/// </summary>
public sealed class RegraEncargosDivida : ValueObject
{
    private RegraEncargosDivida(
        decimal multaMoraPercentual,
        decimal jurosMoraPercentualMensal,
        decimal correcaoPercentualMensal,
        string fundamentoLegal)
    {
        MultaMoraPercentual = multaMoraPercentual;
        JurosMoraPercentualMensal = jurosMoraPercentualMensal;
        CorrecaoPercentualMensal = correcaoPercentualMensal;
        FundamentoLegal = fundamentoLegal;
    }

    /// <summary>Multa de mora (percentual único sobre o valor originário, ex.: 2 para 2%).</summary>
    public decimal MultaMoraPercentual { get; }

    /// <summary>Juros de mora simples ao mês (percentual, ex.: 1 para 1% a.m.).</summary>
    public decimal JurosMoraPercentualMensal { get; }

    /// <summary>Correção monetária ao mês (percentual, ex.: 0.5 para 0,5% a.m.).</summary>
    public decimal CorrecaoPercentualMensal { get; }

    /// <summary>Fundamento legal da regra (artigo do CTM/lei do REFIS).</summary>
    public string FundamentoLegal { get; } = default!;

    /// <summary>Cria uma regra de encargos parametrizada por lei municipal.</summary>
    /// <param name="multaMoraPercentual">Multa de mora (% sobre o originário), não-negativa.</param>
    /// <param name="jurosMoraPercentualMensal">Juros de mora (% a.m.), não-negativos.</param>
    /// <param name="correcaoPercentualMensal">Correção monetária (% a.m.), não-negativa.</param>
    /// <param name="fundamentoLegal">Fundamento legal (CTM/REFIS).</param>
    /// <returns>Nova <see cref="RegraEncargosDivida"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se algum percentual for negativo.</exception>
    /// <exception cref="ArgumentException">Se o fundamento legal estiver vazio.</exception>
    public static RegraEncargosDivida Criar(
        decimal multaMoraPercentual,
        decimal jurosMoraPercentualMensal,
        decimal correcaoPercentualMensal,
        string fundamentoLegal)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(multaMoraPercentual);
        ArgumentOutOfRangeException.ThrowIfNegative(jurosMoraPercentualMensal);
        ArgumentOutOfRangeException.ThrowIfNegative(correcaoPercentualMensal);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentoLegal);
        return new RegraEncargosDivida(multaMoraPercentual, jurosMoraPercentualMensal, correcaoPercentualMensal, fundamentoLegal.Trim());
    }

    /// <summary>
    /// Apura os encargos sobre o valor originário entre o vencimento e a data-base de cálculo. Determinístico:
    /// só depende das DATAS DO FATO (CLAUDE.md §16). Se a data-base for anterior/igual ao vencimento, não há
    /// atraso (apenas o originário). O número de meses em atraso é contado por meses completos decorridos.
    /// </summary>
    /// <param name="valorOriginario">Valor originário inscrito (R$).</param>
    /// <param name="vencimento">Vencimento do crédito (termo inicial dos encargos — LEF art. 2º §5º II).</param>
    /// <param name="dataBaseCalculo">Data-base do cálculo (ex.: data da inscrição, da emissão da CDA).</param>
    /// <returns>Os encargos apurados (correção, multa, juros) e o valor atualizado.</returns>
    /// <exception cref="ArgumentNullException">Se o valor originário for nulo.</exception>
    public EncargosApurados Apurar(ValorMonetario valorOriginario, DateOnly vencimento, DateOnly dataBaseCalculo)
    {
        ArgumentNullException.ThrowIfNull(valorOriginario);

        var mesesAtraso = ContarMesesAtraso(vencimento, dataBaseCalculo);

        // Multa: incidência única sobre o originário (independe do nº de meses), só se houver atraso.
        var multa = mesesAtraso > 0
            ? valorOriginario.AplicarPercentual(MultaMoraPercentual)
            : ValorMonetario.Zero;

        // Juros simples e correção: percentual ao mês × meses em atraso, sobre o originário.
        var juros = valorOriginario.AplicarPercentual(JurosMoraPercentualMensal * mesesAtraso);
        var correcao = valorOriginario.AplicarPercentual(CorrecaoPercentualMensal * mesesAtraso);

        return new EncargosApurados(valorOriginario, correcao, multa, juros);
    }

    private static int ContarMesesAtraso(DateOnly vencimento, DateOnly dataBaseCalculo)
    {
        if (dataBaseCalculo <= vencimento)
        {
            return 0;
        }

        var meses = ((dataBaseCalculo.Year - vencimento.Year) * 12) + (dataBaseCalculo.Month - vencimento.Month);
        if (dataBaseCalculo.Day < vencimento.Day)
        {
            meses--;
        }

        // Em atraso de fração de mês conta-se 1 mês (mora a partir do dia seguinte ao vencimento).
        return Math.Max(meses, 1);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MultaMoraPercentual;
        yield return JurosMoraPercentualMensal;
        yield return CorrecaoPercentualMensal;
        yield return FundamentoLegal;
    }
}
