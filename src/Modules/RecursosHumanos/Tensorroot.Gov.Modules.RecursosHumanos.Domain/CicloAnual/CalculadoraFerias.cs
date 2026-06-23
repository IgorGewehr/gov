namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.CicloAnual;

/// <summary>
/// Resultado PURO do calculo de ferias (design §3.1): remuneracao dos dias gozados, 1/3 constitucional
/// sobre as ferias, abono pecuniario (venda de dias) e o terco proporcional do abono. Imutavel.
/// </summary>
/// <param name="RemuneracaoFerias">Remuneracao proporcional aos dias gozados.</param>
/// <param name="TercoConstitucional">1/3 (parametrizavel) sobre a remuneracao das ferias gozadas (CF art. 7 XVII).</param>
/// <param name="AbonoPecuniario">Valor da venda de dias (abono pecuniario — CLT art. 143); zero se nao houver.</param>
/// <param name="TercoAbono">1/3 proporcional sobre o abono pecuniario; zero se nao houver abono.</param>
public sealed record ResultadoFerias(
    decimal RemuneracaoFerias,
    decimal TercoConstitucional,
    decimal AbonoPecuniario,
    decimal TercoAbono);

/// <summary>
/// Calculadora PURA de ferias: remuneracao do periodo gozado + 1/3 constitucional e, opcionalmente, o
/// abono pecuniario (venda de ate 1/3) com seu terco. A fracao do terco (default 1/3) e PARAMETRO — o
/// ente pode pagar mais, mas nunca menos que o minimo constitucional (design §3.1). Sem relogio: dias e
/// remuneracao mensal sao ENTRADA. As INCIDENCIAS (tributa/nao tributa) vivem na rubrica, nao aqui.
/// </summary>
public static class CalculadoraFerias
{
    /// <summary>Dias-base do mes para o calculo proporcional das ferias (avos diarios).</summary>
    public const int DiasBaseMes = 30;

    /// <summary>Fator de pagamento das ferias gozadas FORA do periodo concessivo, em dobro (CLT art. 137).</summary>
    public const int FatorDobra = 2;

    /// <summary>
    /// Calcula remuneracao de ferias, 1/3 e abono pecuniario.
    /// </summary>
    /// <param name="remuneracaoMensal">Remuneracao mensal-base das ferias (salario + medias habituais).</param>
    /// <param name="diasGozados">Dias de ferias efetivamente gozados (0..30).</param>
    /// <param name="diasVendidos">Dias convertidos em abono pecuniario (0..10); design §3.1.</param>
    /// <param name="fracaoTerco">Fracao do terco constitucional (default 1/3); &gt;= 1/3, &lt;= 1.</param>
    /// <param name="emDobra">
    /// P0-6: quando <c>true</c>, as ferias foram concedidas APOS o periodo concessivo (CLT art. 137) e a
    /// remuneracao dos dias gozados (e o respectivo 1/3) e paga EM DOBRO. O abono pecuniario nao dobra.
    /// </param>
    /// <returns>Composicao das verbas de ferias.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se os dias ou a remuneracao forem invalidos.</exception>
    public static ResultadoFerias Calcular(
        decimal remuneracaoMensal,
        int diasGozados,
        int diasVendidos,
        decimal fracaoTerco,
        bool emDobra = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(remuneracaoMensal);
        ArgumentOutOfRangeException.ThrowIfNegative(diasGozados);
        ArgumentOutOfRangeException.ThrowIfNegative(diasVendidos);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(diasGozados, DiasBaseMes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fracaoTerco);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fracaoTerco, 1m);

        var valorDia = remuneracaoMensal / DiasBaseMes;

        // P0-6: ferias gozadas fora do prazo concessivo sao pagas em dobro (CLT art. 137). O fator e
        // aplicado sobre a remuneracao das ferias e reflete no 1/3; o abono pecuniario nao dobra.
        var fatorDobra = emDobra ? FatorDobra : 1;
        var remuneracaoFerias = decimal.Round(valorDia * diasGozados * fatorDobra, 2, MidpointRounding.AwayFromZero);
        var terco = decimal.Round(remuneracaoFerias * fracaoTerco, 2, MidpointRounding.AwayFromZero);

        var abono = decimal.Round(valorDia * diasVendidos, 2, MidpointRounding.AwayFromZero);
        var tercoAbono = diasVendidos > 0
            ? decimal.Round(abono * fracaoTerco, 2, MidpointRounding.AwayFromZero)
            : 0m;

        return new ResultadoFerias(remuneracaoFerias, terco, abono, tercoAbono);
    }
}
