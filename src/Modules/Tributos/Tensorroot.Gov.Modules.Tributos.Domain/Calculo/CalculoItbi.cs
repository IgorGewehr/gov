using Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;
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
/// Memória de cálculo do ITBI (auditável): valor venal de referência (apenas triagem), valor declarado,
/// base adotada e sua ORIGEM (declarada/arbitrada), alíquota, imposto bruto, isenção e imposto devido.
/// Determinística e fiscalizável. Tema 1.113/STJ. Ver M6-DESIGN §3.1.
/// </summary>
/// <param name="ValorVenalReferencia">Valor venal de referência (motor do Imóvel/PGV) — parâmetro de triagem/alerta, R$.</param>
/// <param name="ValorDeclarado">Valor declarado da transação, R$.</param>
/// <param name="BaseCalculo">Base adotada (R$).</param>
/// <param name="Origem">Origem da base adotada (declarada por padrão; arbitrada só após processo CTN 148).</param>
/// <param name="ProcessoArbitramentoId">Processo de arbitramento vinculado, quando a base foi arbitrada; caso contrário, nulo.</param>
/// <param name="HaDivergenciaReferencia">Verdadeiro se a triagem sinalizou divergência relevante (não altera a base).</param>
/// <param name="AliquotaPercentual">Alíquota aplicada (%).</param>
/// <param name="ImpostoBruto">Imposto antes de isenção (R$).</param>
/// <param name="ValorIsencao">Parcela isenta (R$).</param>
/// <param name="ImpostoDevido">ITBI devido (R$).</param>
public sealed record MemoriaItbi(
    ValorMonetario ValorVenalReferencia,
    ValorMonetario ValorDeclarado,
    ValorMonetario BaseCalculo,
    OrigemBaseCalculoItbi Origem,
    Guid? ProcessoArbitramentoId,
    bool HaDivergenciaReferencia,
    decimal AliquotaPercentual,
    ValorMonetario ImpostoBruto,
    ValorMonetario ValorIsencao,
    ValorMonetario ImpostoDevido);

/// <summary>
/// Serviço de domínio que calcula o ITBI de uma transmissão imobiliária.
/// <para>
/// Base de cálculo = <b>VALOR DECLARADO</b> da transação (Tema 1.113/STJ, REsp 1.937.821: presunção de
/// veracidade do valor declarado). O valor venal de referência <b>NÃO</b> entra na aritmética da base —
/// é apenas parâmetro de <b>triagem/alerta</b> (sinaliza divergência relevante para a revisão fiscal).
/// A elevação da base é uma decisão de <b>processo administrativo</b> (CTN art. 148), nunca aritmética:
/// só ocorre via <see cref="RecalcularComArbitramento"/> com um processo concluído e contraditório.
/// <c>ITBI = base × alíquota − isenção</c>; a alíquota (geral ou SFH) vem da configuração vigente — nada
/// hardcoded. Determinístico e auditável. Ver M6-DESIGN §3.1 e docs/architecture/itbi-tema1113.
/// </para>
/// </summary>
public static class CalculadoraItbi
{
    /// <summary>
    /// Calcula o ITBI de uma transmissão: base = <b>valor declarado</b> (Tema 1.113/STJ). O valor venal
    /// de referência só dispara o alerta de triagem (não altera a base).
    /// </summary>
    /// <param name="valorVenalReferencia">Valor venal de referência (parâmetro de triagem, não é base).</param>
    /// <param name="valorDeclarado">Valor declarado da transação (base de cálculo padrão).</param>
    /// <param name="aliquotaGeralPercentual">Alíquota geral em % (lei municipal vigente).</param>
    /// <param name="aliquotaSfhPercentual">Alíquota da parcela financiada SFH em % (lei municipal vigente).</param>
    /// <param name="margemDivergenciaPercentual">Margem de tolerância da triagem em % (parametrizável por tenant); só dispara alerta.</param>
    /// <param name="parametros">Isenção/uso de alíquota SFH (lei municipal); padrão: nenhum.</param>
    /// <returns>A memória de cálculo do ITBI (origem da base: Declarada).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a isenção ou a margem estiverem fora de [0, 100].</exception>
    public static MemoriaItbi Calcular(
        ValorMonetario valorVenalReferencia,
        ValorMonetario valorDeclarado,
        decimal aliquotaGeralPercentual,
        decimal aliquotaSfhPercentual,
        decimal margemDivergenciaPercentual = 0m,
        ParametrosItbi? parametros = null)
    {
        ArgumentNullException.ThrowIfNull(valorVenalReferencia);
        ArgumentNullException.ThrowIfNull(valorDeclarado);
        parametros ??= ParametrosItbi.Padrao;

        if (parametros.PercentualIsencao is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(parametros), parametros.PercentualIsencao, "O percentual de isenção do ITBI deve estar entre 0 e 100.");
        }

        if (margemDivergenciaPercentual is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(margemDivergenciaPercentual), margemDivergenciaPercentual, "A margem de divergência do ITBI deve estar entre 0 e 100.");
        }

        // Tema 1.113/STJ (tese b): a base de cálculo padrão é o VALOR DECLARADO (presunção de veracidade).
        // O valor venal de referência NÃO entra na aritmética da base — serve apenas a triagem/alerta.
        var baseCalculo = valorDeclarado;

        // Triagem (tese a/c): a referência só pode DEFLAGRAR verificação; nunca elevar a base de ofício.
        var limiteInferior = valorVenalReferencia.Valor * (1m - (margemDivergenciaPercentual / 100m));
        var haDivergenciaReferencia = valorDeclarado.Valor < limiteInferior;

        return Montar(
            valorVenalReferencia,
            valorDeclarado,
            baseCalculo,
            OrigemBaseCalculoItbi.Declarada,
            processoArbitramentoId: null,
            haDivergenciaReferencia,
            aliquotaGeralPercentual,
            aliquotaSfhPercentual,
            parametros);
    }

    /// <summary>
    /// Recalcula o ITBI com base ARBITRADA (CTN art. 148), a partir de um processo administrativo regular
    /// CONCLUÍDO com contraditório. Não há escolha aritmética: a base é o valor arbitrado. O motor não
    /// decide arbitrar — apenas aplica o resultado de um processo auditado.
    /// </summary>
    /// <param name="valorVenalReferencia">Valor venal de referência (mantido na memória como histórico de triagem).</param>
    /// <param name="valorDeclarado">Valor declarado original (mantido para auditoria).</param>
    /// <param name="resultado">Resultado do arbitramento — exige processo concluído.</param>
    /// <param name="aliquotaGeralPercentual">Alíquota geral em % (lei municipal vigente).</param>
    /// <param name="aliquotaSfhPercentual">Alíquota da parcela financiada SFH em % (lei municipal vigente).</param>
    /// <param name="parametros">Isenção/uso de alíquota SFH (lei municipal); padrão: nenhum.</param>
    /// <returns>A memória de cálculo do ITBI (origem da base: ArbitradaArt148).</returns>
    /// <exception cref="InvalidOperationException">Se o processo de arbitramento não estiver concluído.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a isenção estiver fora de [0, 100].</exception>
    public static MemoriaItbi RecalcularComArbitramento(
        ValorMonetario valorVenalReferencia,
        ValorMonetario valorDeclarado,
        ResultadoArbitramento resultado,
        decimal aliquotaGeralPercentual,
        decimal aliquotaSfhPercentual,
        ParametrosItbi? parametros = null)
    {
        ArgumentNullException.ThrowIfNull(valorVenalReferencia);
        ArgumentNullException.ThrowIfNull(valorDeclarado);
        ArgumentNullException.ThrowIfNull(resultado);
        if (!resultado.ProcessoConcluido)
        {
            throw new InvalidOperationException("Arbitramento da base do ITBI exige processo administrativo (CTN art. 148) concluído com contraditório.");
        }

        parametros ??= ParametrosItbi.Padrao;
        if (parametros.PercentualIsencao is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(parametros), parametros.PercentualIsencao, "O percentual de isenção do ITBI deve estar entre 0 e 100.");
        }

        return Montar(
            valorVenalReferencia,
            valorDeclarado,
            resultado.ValorArbitrado,
            OrigemBaseCalculoItbi.ArbitradaArt148,
            resultado.ProcessoArbitramentoId,
            haDivergenciaReferencia: true,
            aliquotaGeralPercentual,
            aliquotaSfhPercentual,
            parametros);
    }

    private static MemoriaItbi Montar(
        ValorMonetario valorVenalReferencia,
        ValorMonetario valorDeclarado,
        ValorMonetario baseCalculo,
        OrigemBaseCalculoItbi origem,
        Guid? processoArbitramentoId,
        bool haDivergenciaReferencia,
        decimal aliquotaGeralPercentual,
        decimal aliquotaSfhPercentual,
        ParametrosItbi parametros)
    {
        var aliquota = parametros.UsarAliquotaSfh ? aliquotaSfhPercentual : aliquotaGeralPercentual;
        var impostoBruto = baseCalculo.AplicarPercentual(aliquota);
        var valorIsencao = impostoBruto.AplicarPercentual(parametros.PercentualIsencao);
        var impostoDevido = ValorMonetario.De(impostoBruto.Valor - valorIsencao.Valor);

        return new MemoriaItbi(
            valorVenalReferencia,
            valorDeclarado,
            baseCalculo,
            origem,
            processoArbitramentoId,
            haDivergenciaReferencia,
            aliquota,
            impostoBruto,
            valorIsencao,
            impostoDevido);
    }
}
