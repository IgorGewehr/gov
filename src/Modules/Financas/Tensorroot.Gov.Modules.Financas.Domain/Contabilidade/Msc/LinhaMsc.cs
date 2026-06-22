using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Msc;

/// <summary>
/// Linha (registro de valor) da Matriz de Saldos Contabeis: uma conta analitica + um
/// <see cref="TipoValorMsc"/> + a natureza do saldo do registro + o valor (sempre &gt;= 0) + as
/// informacoes complementares. E imutavel e nasce valida (valor nao negativo). Derivada do balancete.
/// </summary>
public sealed class LinhaMsc : ValueObject
{
    private LinhaMsc(
        string contaPcasp,
        NaturezaSaldo naturezaSaldo,
        TipoValorMsc tipoValor,
        decimal valor,
        InformacoesComplementaresMsc complementares)
    {
        ContaPcasp = contaPcasp;
        NaturezaSaldo = naturezaSaldo;
        TipoValor = tipoValor;
        Valor = valor;
        Complementares = complementares;
    }

    /// <summary>Codigo PCASP da conta analitica.</summary>
    public string ContaPcasp { get; }

    /// <summary>Natureza do saldo do registro (Devedora/Credora) — define o lado D/C na MSC.</summary>
    public NaturezaSaldo NaturezaSaldo { get; }

    /// <summary>Tipo de valor (SaldoInicial/Movimento/SaldoFinal).</summary>
    public TipoValorMsc TipoValor { get; }

    /// <summary>Valor do registro (sempre &gt;= 0; MSC nao admite negativos).</summary>
    public decimal Valor { get; }

    /// <summary>Informacoes complementares (atributos) da linha.</summary>
    public InformacoesComplementaresMsc Complementares { get; }

    /// <summary>
    /// Cria a linha da MSC. O valor e sempre absoluto (&gt;= 0); a MSC nao admite negativos. Quando o
    /// saldo de uma conta esta no lado contrario ao natural (redutoras/mistas), o chamador inverte a
    /// <paramref name="naturezaSaldo"/> ao inves de usar valor negativo. // TODO(validar-oficial).
    /// </summary>
    /// <param name="contaPcasp">Codigo PCASP da conta.</param>
    /// <param name="naturezaSaldo">Natureza do saldo do registro (D/C).</param>
    /// <param name="tipoValor">Tipo de valor.</param>
    /// <param name="valor">Valor (sera tomado em modulo).</param>
    /// <param name="complementares">Informacoes complementares.</param>
    /// <returns>Nova <see cref="LinhaMsc"/>, ou <c>null</c> se o valor for zero (linha irrelevante).</returns>
    public static LinhaMsc? Criar(
        string contaPcasp,
        NaturezaSaldo naturezaSaldo,
        TipoValorMsc tipoValor,
        decimal valor,
        InformacoesComplementaresMsc complementares)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contaPcasp);
        ArgumentNullException.ThrowIfNull(complementares);

        var absoluto = Math.Abs(valor);
        if (absoluto == 0m)
        {
            return null;
        }

        return new LinhaMsc(contaPcasp, naturezaSaldo, tipoValor, absoluto, complementares);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ContaPcasp;
        yield return NaturezaSaldo;
        yield return TipoValor;
        yield return Valor;
        yield return Complementares;
    }
}
