using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;

/// <summary>Tipo de uso/ocupação do imóvel (entra como fator na PGV e na alíquota IPTU por uso).</summary>
public enum TipoUsoImovel
{
    /// <summary>Residencial.</summary>
    Residencial = 1,

    /// <summary>Comercial.</summary>
    Comercial = 2,

    /// <summary>Industrial.</summary>
    Industrial = 3,

    /// <summary>Serviços.</summary>
    Servicos = 4,

    /// <summary>Misto (residencial + não residencial).</summary>
    Misto = 5,

    /// <summary>Territorial (lote não edificado / sem construção relevante).</summary>
    Territorial = 6,
}

/// <summary>
/// Características físicas do imóvel (Boletim de Cadastro Imobiliário): áreas de terreno e construção,
/// uso, padrão construtivo, ano de construção (para depreciação) e fração ideal de condomínio.
/// O conjunto de campos do BCI é definido por lei municipal; modelamos os recorrentes. Ver M6-DESIGN §1.1.
/// </summary>
public sealed class CaracteristicasImovel : ValueObject
{
    private CaracteristicasImovel(
        decimal areaTerreno,
        decimal areaConstruida,
        TipoUsoImovel tipoUso,
        string padraoConstrutivo,
        int? anoConstrucao,
        decimal fracaoIdeal)
    {
        AreaTerreno = areaTerreno;
        AreaConstruida = areaConstruida;
        TipoUso = tipoUso;
        PadraoConstrutivo = padraoConstrutivo;
        AnoConstrucao = anoConstrucao;
        FracaoIdeal = fracaoIdeal;
    }

    /// <summary>Área do terreno em m² (não-negativa).</summary>
    public decimal AreaTerreno { get; }

    /// <summary>Área construída em m² (não-negativa; zero para lote vago/territorial).</summary>
    public decimal AreaConstruida { get; }

    /// <summary>Tipo de uso/ocupação.</summary>
    public TipoUsoImovel TipoUso { get; }

    /// <summary>
    /// Código do padrão construtivo (ex.: "BAIXO", "MEDIO", "ALTO"). É chave de busca de fator na PGV
    /// (lei municipal) — não há tabela nacional; valores são parametrizados por tenant.
    /// </summary>
    public string PadraoConstrutivo { get; }

    /// <summary>Ano de construção (base do fator de depreciação por idade), quando conhecido.</summary>
    public int? AnoConstrucao { get; }

    /// <summary>
    /// Fração ideal (0 &lt; fração ≤ 1) para unidades autônomas de condomínio; 1 quando não aplicável.
    /// </summary>
    public decimal FracaoIdeal { get; }

    /// <summary>Indica se há construção relevante (área construída maior que zero).</summary>
    public bool Edificado => AreaConstruida > 0m;

    /// <summary>Cria as características do imóvel, validando as invariantes físicas.</summary>
    /// <param name="areaTerreno">Área do terreno em m² (≥ 0).</param>
    /// <param name="areaConstruida">Área construída em m² (≥ 0).</param>
    /// <param name="tipoUso">Tipo de uso.</param>
    /// <param name="padraoConstrutivo">Código do padrão construtivo.</param>
    /// <param name="anoConstrucao">Ano de construção opcional.</param>
    /// <param name="fracaoIdeal">Fração ideal (0 &lt; valor ≤ 1); padrão 1.</param>
    /// <returns>Instância de <see cref="CaracteristicasImovel"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se uma invariante numérica for violada.</exception>
    public static CaracteristicasImovel Criar(
        decimal areaTerreno,
        decimal areaConstruida,
        TipoUsoImovel tipoUso,
        string padraoConstrutivo,
        int? anoConstrucao = null,
        decimal fracaoIdeal = 1m)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(areaTerreno);
        ArgumentOutOfRangeException.ThrowIfNegative(areaConstruida);
        ArgumentException.ThrowIfNullOrWhiteSpace(padraoConstrutivo);
        if (!Enum.IsDefined(tipoUso))
        {
            throw new ArgumentOutOfRangeException(nameof(tipoUso), tipoUso, "Tipo de uso inválido.");
        }

        if (fracaoIdeal <= 0m || fracaoIdeal > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(fracaoIdeal), fracaoIdeal, "A fração ideal deve estar no intervalo (0, 1].");
        }

        if (anoConstrucao is < 1800 or > 3000)
        {
            throw new ArgumentOutOfRangeException(nameof(anoConstrucao), anoConstrucao, "Ano de construção fora do intervalo plausível.");
        }

        return new CaracteristicasImovel(
            decimal.Round(areaTerreno, 2, MidpointRounding.AwayFromZero),
            decimal.Round(areaConstruida, 2, MidpointRounding.AwayFromZero),
            tipoUso,
            padraoConstrutivo.Trim().ToUpperInvariant(),
            anoConstrucao,
            fracaoIdeal);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AreaTerreno;
        yield return AreaConstruida;
        yield return TipoUso;
        yield return PadraoConstrutivo;
        yield return AnoConstrucao;
        yield return FracaoIdeal;
    }
}
