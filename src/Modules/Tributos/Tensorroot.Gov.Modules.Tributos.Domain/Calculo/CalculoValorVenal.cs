using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Calculo;

/// <summary>
/// Memória de cálculo do valor venal (auditável): expõe cada parcela e fator usado, para que o
/// resultado seja determinístico, reproduzível e fiscalizável pelo TCE-RS. Ver M6-DESIGN §1.2.
/// </summary>
/// <param name="AreaTerreno">Área de terreno (m²).</param>
/// <param name="ValorM2Terreno">VUT aplicado (R$/m²).</param>
/// <param name="ValorTerreno">Parcela do terreno (R$).</param>
/// <param name="AreaConstruida">Área construída (m²).</param>
/// <param name="ValorM2Construcao">VUC aplicado (R$/m²).</param>
/// <param name="FatorPadrao">Fator de padrão construtivo aplicado.</param>
/// <param name="FatorDepreciacao">Fator de depreciação aplicado.</param>
/// <param name="FatorUso">Fator de uso/localização aplicado.</param>
/// <param name="ValorConstrucao">Parcela da construção (R$).</param>
/// <param name="FracaoIdeal">Fração ideal aplicada.</param>
/// <param name="ValorVenal">Valor venal final (R$).</param>
public sealed record MemoriaValorVenal(
    decimal AreaTerreno,
    decimal ValorM2Terreno,
    decimal ValorTerreno,
    decimal AreaConstruida,
    decimal ValorM2Construcao,
    decimal FatorPadrao,
    decimal FatorDepreciacao,
    decimal FatorUso,
    decimal ValorConstrucao,
    decimal FracaoIdeal,
    ValorMonetario ValorVenal);

/// <summary>
/// Serviço de domínio que calcula o valor venal de um imóvel a partir da PGV vigente.
/// Fórmula (estrutura genérica — números são da PGV/lei municipal):
/// <c>ValorVenal = (AreaTerreno × VUT) + (AreaConstruida × VUC × FatorPadrao × FatorDepreciacao × FatorUso)</c>,
/// multiplicado pela fração ideal. Determinístico e sem defaults numéricos. Ver M6-DESIGN §1.2.
/// </summary>
public static class CalculadoraValorVenal
{
    /// <summary>Calcula o valor venal de um imóvel segundo a PGV informada.</summary>
    /// <param name="imovel">Imóvel a avaliar.</param>
    /// <param name="planta">PGV vigente do exercício (deve cobrir a zona fiscal do imóvel).</param>
    /// <returns>A memória de cálculo com o valor venal e suas parcelas.</returns>
    /// <exception cref="InvalidOperationException">Se a PGV não estiver vigente ou não cobrir a zona fiscal do imóvel.</exception>
    public static MemoriaValorVenal Calcular(Imovel imovel, PlantaValores planta)
    {
        ArgumentNullException.ThrowIfNull(imovel);
        ArgumentNullException.ThrowIfNull(planta);

        if (!planta.Vigente)
        {
            throw new InvalidOperationException("A PGV informada não está vigente.");
        }

        var caracteristicas = imovel.Caracteristicas;
        var zona = planta.ObterZona(imovel.Endereco.ZonaFiscal)
            ?? throw new InvalidOperationException(
                $"A PGV do exercício {planta.Exercicio} não define valores para a zona fiscal '{imovel.Endereco.ZonaFiscal}'.");

        var valorTerreno = decimal.Round(caracteristicas.AreaTerreno * zona.ValorM2Terreno, 2, MidpointRounding.AwayFromZero);

        var fatorPadrao = planta.ObterFator(TipoFatorPgv.PadraoConstrutivo, caracteristicas.PadraoConstrutivo);
        var fatorDepreciacao = planta.ObterFator(TipoFatorPgv.Depreciacao, FaixaIdade(caracteristicas));
        var fatorUso = planta.ObterFator(TipoFatorPgv.Uso, caracteristicas.TipoUso.ToString());

        var valorConstrucao = decimal.Round(
            caracteristicas.AreaConstruida * zona.ValorM2Construcao * fatorPadrao * fatorDepreciacao * fatorUso,
            2,
            MidpointRounding.AwayFromZero);

        var bruto = (valorTerreno + valorConstrucao) * caracteristicas.FracaoIdeal;
        var valorVenal = ValorMonetario.De(decimal.Round(bruto, 2, MidpointRounding.AwayFromZero));

        return new MemoriaValorVenal(
            caracteristicas.AreaTerreno,
            zona.ValorM2Terreno,
            valorTerreno,
            caracteristicas.AreaConstruida,
            zona.ValorM2Construcao,
            fatorPadrao,
            fatorDepreciacao,
            fatorUso,
            valorConstrucao,
            caracteristicas.FracaoIdeal,
            valorVenal);
    }

    /// <summary>
    /// Deriva a chave de faixa de idade da construção (para o fator de depreciação da PGV).
    /// A granularidade das faixas é definida pela lei municipal; aqui usamos a idade em anos como
    /// chave inteira ("0", "1", ...). // TODO(validar-oficial): faixas de depreciação por idade
    /// conforme a lei da PGV de Maximiliano de Almeida/RS.
    /// </summary>
    private static string? FaixaIdade(CaracteristicasImovel caracteristicas)
    {
        if (caracteristicas.AnoConstrucao is not int ano)
        {
            return null;
        }

        var idade = DateTime.UtcNow.Year - ano;
        return idade < 0 ? "0" : idade.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
