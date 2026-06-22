using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

/// <summary>Identificador forte da <see cref="TabelaRpps"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TabelaRppsId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="TabelaRppsId"/>.</returns>
    public static TabelaRppsId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Parametro de contribuicao do RPPS (servidor efetivo), versionado por competencia/tenant. A
/// aliquota e a base sao definidas em LEI MUNICIPAL do ente (EC 103/2019 art. 9 §4; minimo 14% se
/// ha deficit atuarial, nunca inferior ao RGPS) — NUNCA hardcoded nem default federal (CLAUDE.md
/// S16). Por isso NAO ha seed automatico: o motor opera fail-closed e recusa o calculo de servidor
/// RPPS sem esta tabela carregada para o tenant/competencia. Raiz de agregado.
/// </summary>
public sealed class TabelaRpps : AggregateRoot<TabelaRppsId>, IMustHaveTenant
{
    private readonly List<FaixaProgressiva> _faixas = [];

    private TabelaRpps()
    {
    }

    private TabelaRpps(
        TabelaRppsId id,
        Guid tenantId,
        Competencia vigenciaInicio,
        IReadOnlyList<FaixaProgressiva> faixas,
        decimal? teto,
        string baseLegal)
        : base(id)
    {
        TenantId = tenantId;
        VigenciaInicio = vigenciaInicio;
        _faixas.AddRange(faixas);
        Teto = teto;
        BaseLegal = baseLegal;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Competencia inicial de vigencia (AAAA-MM).</summary>
    public Competencia VigenciaInicio { get; private set; } = default!;

    /// <summary>Teto da base de contribuicao (nulo quando a lei municipal nao fixa teto proprio).</summary>
    public decimal? Teto { get; private set; }

    /// <summary>Fundamento legal (lei previdenciaria municipal) para auditoria/TCE.</summary>
    public string BaseLegal { get; private set; } = default!;

    /// <summary>Faixas progressivas (lei municipal pode ser escalonada) ordenadas por limite.</summary>
    public IReadOnlyList<FaixaProgressiva> Faixas => _faixas;

    /// <summary>Cria a tabela RPPS do tenant a partir da lei previdenciaria municipal.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="vigenciaInicio">Competencia inicial de vigencia.</param>
    /// <param name="faixas">Faixas progressivas (a primeira inicia em zero; contiguas).</param>
    /// <param name="teto">Teto opcional da base.</param>
    /// <param name="baseLegal">Lei previdenciaria municipal.</param>
    /// <returns>Nova <see cref="TabelaRpps"/>.</returns>
    /// <exception cref="ArgumentException">Se faixas/base legal forem invalidas.</exception>
    public static TabelaRpps Criar(
        Guid tenantId,
        Competencia vigenciaInicio,
        IReadOnlyList<FaixaProgressiva> faixas,
        decimal? teto,
        string baseLegal)
    {
        ArgumentNullException.ThrowIfNull(vigenciaInicio);
        ArgumentNullException.ThrowIfNull(faixas);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseLegal);
        if (faixas.Count == 0)
        {
            throw new ArgumentException("Tabela RPPS exige ao menos uma faixa.", nameof(faixas));
        }

        if (faixas[0].LimiteInferior != 0m)
        {
            throw new ArgumentException("A primeira faixa do RPPS deve iniciar em zero.", nameof(faixas));
        }

        for (var i = 1; i < faixas.Count; i++)
        {
            if (faixas[i].LimiteInferior != faixas[i - 1].LimiteSuperior)
            {
                throw new ArgumentException("As faixas do RPPS devem ser contiguas (sem lacunas/sobreposicoes).", nameof(faixas));
            }
        }

        if (teto is { } limite)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limite);
        }

        return new TabelaRpps(
            TabelaRppsId.New(),
            tenantId,
            vigenciaInicio,
            faixas,
            teto,
            baseLegal.Trim());
    }

    /// <summary>
    /// Calcula a contribuicao do RPPS sobre a base, de forma progressiva e cumulativa, respeitando
    /// o teto municipal quando definido. Deterministico.
    /// </summary>
    /// <param name="baseContribuicao">Soma das rubricas com incidencia de RPPS.</param>
    /// <returns>Valor do desconto de RPPS (2 casas), nao-negativo.</returns>
    public decimal CalcularContribuicao(decimal baseContribuicao)
    {
        if (baseContribuicao <= 0m)
        {
            return 0m;
        }

        var baseLimitada = Teto is { } teto && baseContribuicao > teto ? teto : baseContribuicao;
        var total = 0m;
        foreach (var faixa in _faixas)
        {
            total += faixa.Contribuicao(baseLimitada);
        }

        return decimal.Round(total, 2, MidpointRounding.AwayFromZero);
    }
}
