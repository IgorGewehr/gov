using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Calculo;

/// <summary>
/// Memória de cálculo do IPTU (auditável): valor venal, alíquota aplicada, imposto bruto, isenção/
/// desconto e imposto devido. Determinístico e fiscalizável. Ver M6-DESIGN §1.3.
/// </summary>
/// <param name="ValorVenal">Memória do valor venal.</param>
/// <param name="AliquotaPercentual">Alíquota aplicada (%).</param>
/// <param name="ImpostoBruto">Imposto antes de isenções/descontos (R$).</param>
/// <param name="ValorIsencao">Parcela isenta (R$).</param>
/// <param name="ValorDesconto">Desconto aplicado (R$).</param>
/// <param name="ImpostoDevido">IPTU devido após isenções/descontos (R$).</param>
public sealed record MemoriaIptu(
    MemoriaValorVenal ValorVenal,
    decimal AliquotaPercentual,
    ValorMonetario ImpostoBruto,
    ValorMonetario ValorIsencao,
    ValorMonetario ValorDesconto,
    ValorMonetario ImpostoDevido);

/// <summary>
/// Parâmetros de isenção/desconto aplicáveis ao IPTU de um imóvel (definidos por lei municipal —
/// nunca hardcoded). Modelados como entrada do motor para mantê-lo determinístico e parametrizável.
/// // TODO(validar-oficial): isenções/imunidades e descontos (cota única/adimplência) conforme o
/// CTM/decreto de Maximiliano de Almeida/RS.
/// </summary>
/// <param name="PercentualIsencao">Percentual de isenção sobre o imposto bruto (0 a 100).</param>
/// <param name="PercentualDesconto">Percentual de desconto sobre o saldo após isenção (0 a 100).</param>
public sealed record ParametrosIsencaoIptu(decimal PercentualIsencao = 0m, decimal PercentualDesconto = 0m)
{
    /// <summary>Sem isenção nem desconto.</summary>
    public static ParametrosIsencaoIptu Nenhuma { get; } = new();
}

/// <summary>
/// Serviço de domínio que apura o IPTU de um imóvel: <c>IPTU = ValorVenal × alíquota − isenções −
/// descontos</c>. A alíquota vem da <see cref="TabelaAliquotaIptu"/> vigente (única ou progressiva
/// por faixa de valor venal). Determinístico, parametrizado e auditável. Ver M6-DESIGN §1.3.
/// </summary>
public static class CalculadoraIptu
{
    /// <summary>Apura o IPTU de um imóvel.</summary>
    /// <param name="imovel">Imóvel a tributar.</param>
    /// <param name="planta">PGV vigente do exercício.</param>
    /// <param name="tabelaAliquota">Tabela de alíquotas vigente do exercício.</param>
    /// <param name="isencao">Parâmetros de isenção/desconto (lei municipal); padrão: nenhum.</param>
    /// <returns>A memória de cálculo do IPTU.</returns>
    /// <exception cref="InvalidOperationException">Se a tabela de alíquotas não estiver vigente.</exception>
    public static MemoriaIptu Calcular(
        Imovel imovel,
        PlantaValores planta,
        TabelaAliquotaIptu tabelaAliquota,
        ParametrosIsencaoIptu? isencao = null)
    {
        ArgumentNullException.ThrowIfNull(imovel);
        ArgumentNullException.ThrowIfNull(tabelaAliquota);
        isencao ??= ParametrosIsencaoIptu.Nenhuma;

        if (!tabelaAliquota.Vigente)
        {
            throw new InvalidOperationException("A tabela de alíquotas informada não está vigente.");
        }

        if (isencao.PercentualIsencao is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(isencao), isencao.PercentualIsencao, "O percentual de isenção deve estar entre 0 e 100.");
        }

        if (isencao.PercentualDesconto is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(isencao), isencao.PercentualDesconto, "O percentual de desconto deve estar entre 0 e 100.");
        }

        var memoriaVenal = CalculadoraValorVenal.Calcular(imovel, planta);
        var valorVenal = memoriaVenal.ValorVenal.Valor;

        var aliquota = tabelaAliquota.AliquotaPara(valorVenal);
        var impostoBruto = ValorMonetario.De(decimal.Round(valorVenal * aliquota / 100m, 2, MidpointRounding.AwayFromZero));

        var valorIsencao = impostoBruto.AplicarPercentual(isencao.PercentualIsencao);
        var aposIsencao = ValorMonetario.De(impostoBruto.Valor - valorIsencao.Valor);

        var valorDesconto = aposIsencao.AplicarPercentual(isencao.PercentualDesconto);
        var impostoDevido = ValorMonetario.De(aposIsencao.Valor - valorDesconto.Valor);

        return new MemoriaIptu(
            memoriaVenal,
            aliquota,
            impostoBruto,
            valorIsencao,
            valorDesconto,
            impostoDevido);
    }
}
