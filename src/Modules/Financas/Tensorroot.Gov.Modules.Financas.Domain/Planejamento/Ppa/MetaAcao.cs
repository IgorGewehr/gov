using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;

/// <summary>
/// Meta de uma ação do PPA num ano do quadriênio (física e financeira), com
/// regionalização (CF 165 §1º exige PPA regionalizado). Entidade-filha de <see cref="AcaoPpa"/>.
/// </summary>
public sealed class MetaAcao : Entity<MetaAcaoId>
{
    private MetaAcao()
    {
    }

    private MetaAcao(
        MetaAcaoId id,
        AcaoPpaId acaoId,
        int ano,
        decimal metaFisica,
        string unidadeMedida,
        ValorMonetario metaFinanceira,
        string regiao)
        : base(id)
    {
        AcaoId = acaoId;
        Ano = ano;
        MetaFisica = metaFisica;
        UnidadeMedida = unidadeMedida;
        MetaFinanceira = metaFinanceira;
        Regiao = regiao;
    }

    /// <summary>Ação à qual a meta pertence.</summary>
    public AcaoPpaId AcaoId { get; private set; }

    /// <summary>Ano do quadriênio a que a meta se refere.</summary>
    public int Ano { get; private set; }

    /// <summary>Meta física (quantidade do produto).</summary>
    public decimal MetaFisica { get; private set; }

    /// <summary>Unidade de medida da meta física (ex.: "km", "unidade").</summary>
    public string UnidadeMedida { get; private set; } = default!;

    /// <summary>Meta financeira (valor previsto para o ano).</summary>
    public ValorMonetario MetaFinanceira { get; private set; } = default!;

    /// <summary>Região de regionalização da meta (ex.: "Sede", "Zona Rural").</summary>
    public string Regiao { get; private set; } = default!;

    /// <summary>Cria uma meta válida para um ano do quadriênio.</summary>
    /// <param name="acaoId">Ação dona da meta.</param>
    /// <param name="ano">Ano do quadriênio.</param>
    /// <param name="metaFisica">Meta física (quantidade não-negativa).</param>
    /// <param name="unidadeMedida">Unidade de medida.</param>
    /// <param name="metaFinanceira">Meta financeira.</param>
    /// <param name="regiao">Região.</param>
    /// <returns>Nova <see cref="MetaAcao"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a meta física for negativa.</exception>
    internal static MetaAcao Criar(
        AcaoPpaId acaoId,
        int ano,
        decimal metaFisica,
        string unidadeMedida,
        ValorMonetario metaFinanceira,
        string regiao)
    {
        ArgumentNullException.ThrowIfNull(metaFinanceira);
        ArgumentException.ThrowIfNullOrWhiteSpace(unidadeMedida);
        ArgumentException.ThrowIfNullOrWhiteSpace(regiao);
        ArgumentOutOfRangeException.ThrowIfNegative(metaFisica);

        return new MetaAcao(
            MetaAcaoId.New(),
            acaoId,
            ano,
            metaFisica,
            unidadeMedida.Trim(),
            metaFinanceira,
            regiao.Trim());
    }

    /// <summary>Atualiza os valores físico/financeiro da meta (re-definição idempotente por ano).</summary>
    /// <param name="metaFisica">Nova meta física.</param>
    /// <param name="unidadeMedida">Nova unidade de medida.</param>
    /// <param name="metaFinanceira">Nova meta financeira.</param>
    /// <param name="regiao">Nova região.</param>
    internal void Atualizar(decimal metaFisica, string unidadeMedida, ValorMonetario metaFinanceira, string regiao)
    {
        ArgumentNullException.ThrowIfNull(metaFinanceira);
        ArgumentException.ThrowIfNullOrWhiteSpace(unidadeMedida);
        ArgumentException.ThrowIfNullOrWhiteSpace(regiao);
        ArgumentOutOfRangeException.ThrowIfNegative(metaFisica);

        MetaFisica = metaFisica;
        UnidadeMedida = unidadeMedida.Trim();
        MetaFinanceira = metaFinanceira;
        Regiao = regiao.Trim();
    }
}
