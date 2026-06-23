namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;

/// <summary>
/// Percentuais legais de margem consignavel POR BALDE, parametrizaveis por tenant/vigencia (Lei 14.131/2021;
/// nunca <em>hardcoded</em> — CLAUDE.md S7/S16). Defaults documentais espelham a lei federal:
/// 35% geral + 5% cartao de credito consignado + 5% cartao beneficio (= 45% total). Objeto de valor imutavel.
/// </summary>
/// <param name="PercentualGeral">Fracao (0..1) da margem GERAL. Default legal 0,35.</param>
/// <param name="PercentualCartaoConsignado">Fracao (0..1) da reserva de CARTAO consignado. Default legal 0,05.</param>
/// <param name="PercentualCartaoBeneficio">Fracao (0..1) da reserva de CARTAO BENEFICIO. Default legal 0,05.</param>
public sealed record PercentuaisMargem(
    decimal PercentualGeral,
    decimal PercentualCartaoConsignado,
    decimal PercentualCartaoBeneficio)
{
    /// <summary>Defaults legais (Lei 14.131/2021): 35% + 5% + 5%. Substituiveis por parametro do tenant.</summary>
    public static PercentuaisMargem PadraoLegal => new(0.35m, 0.05m, 0.05m);

    /// <summary>Fracao do balde informado.</summary>
    /// <param name="grupo">Balde de margem.</param>
    /// <returns>Fracao (0..1) do balde.</returns>
    public decimal Do(GrupoMargem grupo) => grupo switch
    {
        GrupoMargem.Geral => PercentualGeral,
        GrupoMargem.CartaoConsignado => PercentualCartaoConsignado,
        GrupoMargem.CartaoBeneficio => PercentualCartaoBeneficio,
        _ => 0m,
    };
}

/// <summary>
/// MARGEM CONSIGNAVEL de um servidor numa competencia (dominio puro, sem I/O — analogo a
/// <c>EfeitoFolhaAfastamento</c>): a partir da BASE DE CALCULO (remuneracao-base consignavel apurada da
/// folha — proventos com <see cref="RubricaConsignavel.ContaParaMargem"/>) e dos percentuais por balde
/// (parametrizaveis por tenant), expoe o LIMITE, o COMPROMETIDO e o DISPONIVEL de cada balde (reserva legal).
/// Tres baldes INDEPENDENTES: o cartao (5%+5%) nao invade o geral (35%) e vice-versa. Deterministico.
/// </summary>
public sealed class MargemConsignavel
{
    private readonly decimal _baseDeCalculo;
    private readonly PercentuaisMargem _percentuais;
    private readonly Dictionary<GrupoMargem, decimal> _comprometido;

    private MargemConsignavel(
        decimal baseDeCalculo,
        PercentuaisMargem percentuais,
        Dictionary<GrupoMargem, decimal> comprometido)
    {
        _baseDeCalculo = baseDeCalculo;
        _percentuais = percentuais;
        _comprometido = comprometido;
    }

    /// <summary>Base de calculo (remuneracao-base consignavel da competencia), nunca negativa.</summary>
    public decimal BaseDeCalculo => _baseDeCalculo;

    /// <summary>
    /// Cria a margem do servidor a partir da base consignavel, dos percentuais por balde e do total ja
    /// comprometido por balde (somatorio das parcelas das consignacoes Averbadas vigentes).
    /// </summary>
    /// <param name="baseDeCalculo">Remuneracao-base consignavel da competencia (proventos que contam para margem).</param>
    /// <param name="percentuais">Percentuais legais por balde (parametrizaveis por tenant).</param>
    /// <param name="comprometidoPorGrupo">Valor ja comprometido por balde (default zero por balde ausente).</param>
    /// <returns>Margem consignavel calculada.</returns>
    /// <exception cref="ArgumentNullException">Se os percentuais forem nulos.</exception>
    public static MargemConsignavel Calcular(
        decimal baseDeCalculo,
        PercentuaisMargem percentuais,
        IReadOnlyDictionary<GrupoMargem, decimal>? comprometidoPorGrupo = null)
    {
        ArgumentNullException.ThrowIfNull(percentuais);
        var baseNaoNegativa = baseDeCalculo < 0m ? 0m : baseDeCalculo;
        var comprometido = new Dictionary<GrupoMargem, decimal>
        {
            [GrupoMargem.Geral] = 0m,
            [GrupoMargem.CartaoConsignado] = 0m,
            [GrupoMargem.CartaoBeneficio] = 0m,
        };
        if (comprometidoPorGrupo is not null)
        {
            foreach (var (grupo, valor) in comprometidoPorGrupo)
            {
                comprometido[grupo] = comprometido.TryGetValue(grupo, out var atual) ? atual + valor : valor;
            }
        }

        return new MargemConsignavel(baseNaoNegativa, percentuais, comprometido);
    }

    /// <summary>Limite (teto) do balde = base x percentual legal do balde (2 casas).</summary>
    /// <param name="grupo">Balde de margem.</param>
    /// <returns>Valor maximo consignavel no balde.</returns>
    public decimal Limite(GrupoMargem grupo)
        => decimal.Round(_baseDeCalculo * _percentuais.Do(grupo), 2, MidpointRounding.AwayFromZero);

    /// <summary>Valor ja comprometido (consumido) no balde.</summary>
    /// <param name="grupo">Balde de margem.</param>
    /// <returns>Total ja averbado no balde (2 casas).</returns>
    public decimal Comprometido(GrupoMargem grupo)
        => decimal.Round(_comprometido.TryGetValue(grupo, out var valor) ? valor : 0m, 2, MidpointRounding.AwayFromZero);

    /// <summary>Margem disponivel do balde = limite - comprometido, nunca negativa.</summary>
    /// <param name="grupo">Balde de margem.</param>
    /// <returns>Valor ainda consignavel no balde (2 casas).</returns>
    public decimal Disponivel(GrupoMargem grupo)
    {
        var disponivel = Limite(grupo) - Comprometido(grupo);
        return disponivel < 0m ? 0m : decimal.Round(disponivel, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Indica se cabe uma nova parcela de <paramref name="valorParcela"/> no balde (pre-condicao de averbacao).</summary>
    /// <param name="grupo">Balde de margem.</param>
    /// <param name="valorParcela">Valor da parcela a averbar.</param>
    /// <returns><c>true</c> se a parcela cabe na margem disponivel do balde.</returns>
    public bool ComportaParcela(GrupoMargem grupo, decimal valorParcela)
        => valorParcela > 0m && valorParcela <= Disponivel(grupo);
}
