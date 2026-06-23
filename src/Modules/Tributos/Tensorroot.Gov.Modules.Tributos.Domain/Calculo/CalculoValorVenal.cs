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
        var fatorDepreciacao = FatorDepreciacao(caracteristicas, planta);
        var fatorUso = planta.ObterFator(TipoFatorPgv.Uso, caracteristicas.TipoUso.ToString());

        var valorConstrucao = decimal.Round(
            caracteristicas.AreaConstruida * zona.ValorM2Construcao * fatorPadrao * fatorDepreciacao * fatorUso,
            2,
            MidpointRounding.AwayFromZero);

        // Convenção de FracaoIdeal (IP-B3): a fração ideal incide sobre o VALOR VENAL TOTAL da unidade
        // autônoma (terreno comum + construção). Premissa do BCI: `AreaConstruida` registra a área
        // PRIVATIVA da unidade (não a do prédio inteiro) e `AreaTerreno` o terreno comum do condomínio;
        // a fração rateia o conjunto. Para imóvel não-condominial, `FracaoIdeal = 1` é neutro.
        // // TODO(validar-oficial): confirmar contra o CTM/lei do condomínio de Maximiliano de Almeida/RS
        // se a fração ideal incide sobre o total (terreno + construção) ou somente sobre o terreno comum.
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
    /// Calcula o fator de depreciação da construção a partir da PGV, casando a idade por INTERVALO de
    /// faixa (ver <see cref="FaixaDepreciacao"/>). A idade é derivada do EXERCÍCIO do fato gerador
    /// (<see cref="PlantaValores.Exercicio"/>) — NUNCA do relógio (<c>DateTime.UtcNow</c>) — para que a
    /// reapuração do mesmo lançamento em outro ano produza SEMPRE o mesmo valor (reprodutibilidade
    /// inegociável, CLAUDE.md §16; cabeçalho da memória de cálculo). Sem ano de construção conhecido,
    /// não há depreciação a aplicar → fator neutro 1.
    /// </summary>
    /// <param name="caracteristicas">Características do imóvel (ano de construção).</param>
    /// <param name="planta">PGV vigente do exercício do fato gerador.</param>
    /// <returns>O multiplicador de depreciação.</returns>
    private static decimal FatorDepreciacao(CaracteristicasImovel caracteristicas, PlantaValores planta)
    {
        // Sem construção (lote territorial), não há o que depreciar: fator neutro. Evita exigir
        // faixa de depreciação para imóvel sem edificação (a parcela de construção é 0 de qualquer modo).
        if (caracteristicas.AreaConstruida <= 0m || caracteristicas.AnoConstrucao is not int ano)
        {
            return 1m;
        }

        // Idade pelo exercício do fato gerador (determinístico). Construção futura (ano > exercício)
        // é clampada a idade 0 — o cadastro já impede ano fora de [1800, 3000].
        var idade = Math.Max(0, planta.Exercicio - ano);
        return planta.ObterFatorDepreciacaoPorIdade(idade);
    }
}
