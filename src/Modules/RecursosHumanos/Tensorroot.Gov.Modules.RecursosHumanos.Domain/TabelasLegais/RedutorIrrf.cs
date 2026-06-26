using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

/// <summary>
/// Redutor mensal do IRRF instituido pela Lei 15.270/2025 (vigencia 01/01/2026), que amplia a faixa de
/// nao-tributacao reduzindo o imposto apurado pela tabela progressiva. Modelo linear PARAMETRIZADO
/// (coeficientes legais por exercicio — NUNCA hardcoded no motor, CLAUDE.md S7/S16):
/// <para>
/// <c>redutor = min(TetoRedutor; max(0; CoeficienteBase - CoeficienteRendimento x rendimentoBruto))</c>,
/// aplicado somente quando <c>rendimentoBruto &lt;= LimiteRendimento</c>; acima do limite o redutor e zero
/// (Lei 15.270/2025, novo art. 3o-A da Lei 9.250/1995, §§ 1o e 2o). O redutor e ainda limitado ao proprio
/// imposto apurado (§ 1o) — esse teto e aplicado por <see cref="TabelaIrrf"/>, que conhece o imposto.
/// </para>
/// Para 2026 (RFB): CoeficienteBase = 978,62 · CoeficienteRendimento = 0,133145 · TetoRedutor = 312,89 ·
/// LimiteRendimento = 7.350,00 (em R$ 5.000,00 a formula resulta em ~312,895, coberto pelo teto 312,89).
/// </summary>
public sealed class RedutorIrrf : ValueObject
{
    private RedutorIrrf(
        decimal coeficienteBase,
        decimal coeficienteRendimento,
        decimal tetoRedutor,
        decimal limiteRendimento)
    {
        CoeficienteBase = coeficienteBase;
        CoeficienteRendimento = coeficienteRendimento;
        TetoRedutor = tetoRedutor;
        LimiteRendimento = limiteRendimento;
    }

    /// <summary>Termo constante da formula linear (R$). Em 2026: 978,62.</summary>
    public decimal CoeficienteBase { get; }

    /// <summary>Coeficiente angular aplicado ao rendimento bruto (fracao). Em 2026: 0,133145.</summary>
    public decimal CoeficienteRendimento { get; }

    /// <summary>Teto absoluto do redutor (R$). Em 2026: 312,89.</summary>
    public decimal TetoRedutor { get; }

    /// <summary>Rendimento bruto mensal maximo (R$) para fruicao do redutor. Em 2026: 7.350,00.</summary>
    public decimal LimiteRendimento { get; }

    /// <summary>Cria o redutor mensal do IRRF com coeficientes legais parametrizados.</summary>
    /// <param name="coeficienteBase">Termo constante (R$).</param>
    /// <param name="coeficienteRendimento">Coeficiente angular (fracao).</param>
    /// <param name="tetoRedutor">Teto absoluto do redutor (R$).</param>
    /// <param name="limiteRendimento">Rendimento bruto maximo para fruicao (R$).</param>
    /// <returns>Instancia valida de <see cref="RedutorIrrf"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se algum coeficiente for negativo ou o limite/teto nao for positivo.</exception>
    public static RedutorIrrf De(
        decimal coeficienteBase,
        decimal coeficienteRendimento,
        decimal tetoRedutor,
        decimal limiteRendimento)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(coeficienteBase);
        ArgumentOutOfRangeException.ThrowIfNegative(coeficienteRendimento);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tetoRedutor);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limiteRendimento);
        return new RedutorIrrf(coeficienteBase, coeficienteRendimento, tetoRedutor, limiteRendimento);
    }

    /// <summary>
    /// Calcula o redutor (antes do teto pelo imposto, aplicado pela tabela) para o rendimento bruto mensal.
    /// Zero quando o rendimento excede <see cref="LimiteRendimento"/>; caso contrario, a formula linear
    /// limitada inferiormente a zero e superiormente a <see cref="TetoRedutor"/>.
    /// </summary>
    /// <param name="rendimentoBruto">Rendimento tributavel bruto mensal (sem deducoes).</param>
    /// <returns>Valor do redutor (2 casas), nao-negativo.</returns>
    public decimal Calcular(decimal rendimentoBruto)
    {
        if (rendimentoBruto <= 0m || rendimentoBruto > LimiteRendimento)
        {
            return 0m;
        }

        var bruto = CoeficienteBase - (CoeficienteRendimento * rendimentoBruto);
        var limitado = Math.Clamp(bruto, 0m, TetoRedutor);
        return decimal.Round(limitado, 2, MidpointRounding.AwayFromZero);
    }

    /// <inheritdoc />
    public override string ToString()
        => string.Create(
            CultureInfo.InvariantCulture,
            $"redutor=min({TetoRedutor:0.00};{CoeficienteBase:0.00}-{CoeficienteRendimento:0.000000}*R) ate R={LimiteRendimento:0.00}");

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CoeficienteBase;
        yield return CoeficienteRendimento;
        yield return TetoRedutor;
        yield return LimiteRendimento;
    }
}
