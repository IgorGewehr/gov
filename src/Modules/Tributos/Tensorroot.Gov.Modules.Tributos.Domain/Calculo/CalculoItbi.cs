using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Calculo;

/// <summary>
/// Parâmetros de isenção/imunidade aplicáveis ao ITBI de uma transmissão (definidos por lei municipal
/// e pela CF — nunca hardcoded). Modelados como entrada para manter o motor determinístico.
/// // TODO(validar-oficial): hipóteses (1ª aquisição SFH, imunidades CF art. 156 §2º I) conforme o
/// CTM de Maximiliano de Almeida/RS (M6-DESIGN §3.1).
/// </summary>
/// <param name="PercentualIsencao">Percentual de isenção sobre o imposto (0 a 100).</param>
/// <param name="UsarAliquotaSfh">Se verdadeiro, aplica a alíquota reduzida do SFH sobre a base.</param>
public sealed record ParametrosItbi(decimal PercentualIsencao = 0m, bool UsarAliquotaSfh = false)
{
    /// <summary>Sem isenção e alíquota geral.</summary>
    public static ParametrosItbi Padrao { get; } = new();
}

/// <summary>
/// Memória de cálculo do ITBI (auditável): valor venal de referência, valor declarado, base adotada
/// (e sua origem), alíquota, imposto bruto, isenção e imposto devido. Determinística e fiscalizável.
/// Ver M6-DESIGN §3.1.
/// </summary>
/// <param name="ValorVenalReferencia">Valor venal de referência (motor do Imóvel/PGV), R$.</param>
/// <param name="ValorDeclarado">Valor declarado da transação, R$.</param>
/// <param name="BaseCalculo">Base adotada (R$).</param>
/// <param name="BaseFoiValorVenal">Verdadeiro se a base adotada foi o valor venal (maior que o declarado).</param>
/// <param name="AliquotaPercentual">Alíquota aplicada (%).</param>
/// <param name="ImpostoBruto">Imposto antes de isenção (R$).</param>
/// <param name="ValorIsencao">Parcela isenta (R$).</param>
/// <param name="ImpostoDevido">ITBI devido (R$).</param>
public sealed record MemoriaItbi(
    ValorMonetario ValorVenalReferencia,
    ValorMonetario ValorDeclarado,
    ValorMonetario BaseCalculo,
    bool BaseFoiValorVenal,
    decimal AliquotaPercentual,
    ValorMonetario ImpostoBruto,
    ValorMonetario ValorIsencao,
    ValorMonetario ImpostoDevido);

/// <summary>
/// Serviço de domínio que calcula o ITBI de uma transmissão imobiliária.
/// <para>
/// Base de cálculo = <b>MAIOR</b> entre o valor venal de referência (reusa o motor do Imóvel/PGV) e o
/// valor declarado da transação; <c>ITBI = base × alíquota − isenção</c>. A alíquota (geral ou SFH) vem
/// da <see cref="Itbi.AliquotaItbi"/> vigente — nada hardcoded. Determinístico e auditável. Ver M6-DESIGN §3.1.
/// </para>
/// <para>
/// // TODO(validar-oficial): o Tema 1.113/STJ (REsp 1.937.821) estabelece que a base do ITBI é o valor
/// de mercado da transação, com presunção a favor do <b>valor declarado</b> (não vinculado ao valor venal
/// do IPTU e sem arbitramento unilateral por valor de referência, salvo processo administrativo CTN art. 148).
/// Esta implementação segue a regra "base = MAIOR valor" do M6-DESIGN/spec; a conciliação com o Tema 1.113
/// (valor de referência meramente indicativo + workflow de arbitramento) deve ser validada com a
/// procuradoria do município antes de produção.
/// </para>
/// </summary>
public static class CalculadoraItbi
{
    /// <summary>Calcula o ITBI de uma transmissão: base = maior(valor venal, valor declarado).</summary>
    /// <param name="valorVenalReferencia">Valor venal de referência do imóvel (do motor de valor venal).</param>
    /// <param name="valorDeclarado">Valor declarado da transação.</param>
    /// <param name="aliquotaGeralPercentual">Alíquota geral em % (lei municipal vigente).</param>
    /// <param name="aliquotaSfhPercentual">Alíquota da parcela financiada SFH em % (lei municipal vigente).</param>
    /// <param name="parametros">Isenção/uso de alíquota SFH (lei municipal); padrão: nenhum.</param>
    /// <returns>A memória de cálculo do ITBI.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a isenção estiver fora de [0, 100].</exception>
    public static MemoriaItbi Calcular(
        ValorMonetario valorVenalReferencia,
        ValorMonetario valorDeclarado,
        decimal aliquotaGeralPercentual,
        decimal aliquotaSfhPercentual,
        ParametrosItbi? parametros = null)
    {
        ArgumentNullException.ThrowIfNull(valorVenalReferencia);
        ArgumentNullException.ThrowIfNull(valorDeclarado);
        parametros ??= ParametrosItbi.Padrao;

        if (parametros.PercentualIsencao is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(parametros), parametros.PercentualIsencao, "O percentual de isenção do ITBI deve estar entre 0 e 100.");
        }

        var baseFoiValorVenal = valorVenalReferencia.Valor >= valorDeclarado.Valor;
        var baseCalculo = baseFoiValorVenal ? valorVenalReferencia : valorDeclarado;

        var aliquota = parametros.UsarAliquotaSfh ? aliquotaSfhPercentual : aliquotaGeralPercentual;
        var impostoBruto = baseCalculo.AplicarPercentual(aliquota);

        var valorIsencao = impostoBruto.AplicarPercentual(parametros.PercentualIsencao);
        var impostoDevido = ValorMonetario.De(impostoBruto.Valor - valorIsencao.Valor);

        return new MemoriaItbi(
            valorVenalReferencia,
            valorDeclarado,
            baseCalculo,
            baseFoiValorVenal,
            aliquota,
            impostoBruto,
            valorIsencao,
            impostoDevido);
    }
}
