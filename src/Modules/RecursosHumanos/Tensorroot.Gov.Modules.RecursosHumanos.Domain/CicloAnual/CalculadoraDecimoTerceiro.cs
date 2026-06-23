namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.CicloAnual;

/// <summary>
/// Calculadora PURA do valor do 13o salario (gratificacao natalina): <c>valor = (remuneracaoBase / 12) x
/// avos</c>, regra de proporcionalidade por avos da Lei 4.090/62 (design §2.1). A remuneracaoBase (salario
/// + medias de variaveis habituais que integram o 13o) e montada pelo handler a partir do catalogo de
/// rubricas — aqui nao ha numero hardcoded. Deterministica: funcao de (base, avos).
/// </summary>
public static class CalculadoraDecimoTerceiro
{
    /// <summary>Numero de avos que compoem o 13o integral (12 meses).</summary>
    public const int AvosNoAno = 12;

    /// <summary>
    /// Calcula o valor INTEGRAL do 13o proporcional aos avos: <c>(remuneracaoBase / 12) x avos</c>.
    /// </summary>
    /// <param name="remuneracaoBase">Remuneracao-base do 13o (salario + medias habituais).</param>
    /// <param name="avos">Avos apurados no ano-calendario (ou ate o desligamento, na rescisao).</param>
    /// <returns>Valor integral do 13o (2 casas), nao-negativo.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a remuneracao-base for negativa.</exception>
    public static decimal CalcularIntegral(decimal remuneracaoBase, Avos avos)
    {
        ArgumentNullException.ThrowIfNull(avos);
        ArgumentOutOfRangeException.ThrowIfNegative(remuneracaoBase);
        if (avos.Quantidade <= 0)
        {
            return 0m;
        }

        var valor = (remuneracaoBase / AvosNoAno) * avos.Quantidade;
        return decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calcula a parcela do 13o aplicando o percentual da parcela (ex.: 1a parcela = 50%) sobre o valor
    /// integral. O percentual e PARAMETRO (design §2.3), nunca hardcoded.
    /// </summary>
    /// <param name="valorIntegral">Valor integral do 13o (de <see cref="CalcularIntegral"/>).</param>
    /// <param name="percentual">Fracao da parcela (0 &lt; p &lt;= 1).</param>
    /// <returns>Valor da parcela (2 casas).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o percentual estiver fora de (0, 1].</exception>
    public static decimal CalcularParcela(decimal valorIntegral, decimal percentual)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(percentual);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentual, 1m);
        ArgumentOutOfRangeException.ThrowIfNegative(valorIntegral);
        return decimal.Round(valorIntegral * percentual, 2, MidpointRounding.AwayFromZero);
    }
}
