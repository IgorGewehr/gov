using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

/// <summary>
/// Tabela progressiva de contribuicao previdenciaria do RGPS (INSS), versionada por competencia e
/// tenant. As faixas e o teto sao PARAMETROS LEGAIS por exercicio (Portarias Interministeriais
/// MPS/MF) — NUNCA hardcoded no motor (CLAUDE.md S7/S16): o seed traz os valores oficiais, mas o
/// dado e mutavel por tenant/competencia. Raiz de agregado.
/// </summary>
public sealed class TabelaInss : AggregateRoot<TabelaInssId>, IMustHaveTenant
{
    private readonly List<FaixaProgressiva> _faixas = [];

    private TabelaInss()
    {
    }

    private TabelaInss(
        TabelaInssId id,
        Guid tenantId,
        Competencia vigenciaInicio,
        IReadOnlyList<FaixaProgressiva> faixas,
        decimal teto,
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

    /// <summary>Competencia inicial de vigencia (AAAA-MM); aplica-se ate a proxima tabela vigente.</summary>
    public Competencia VigenciaInicio { get; private set; } = default!;

    /// <summary>Teto do salario de contribuicao (limite maximo da base).</summary>
    public decimal Teto { get; private set; }

    /// <summary>Fundamento legal (ex.: Portaria Interministerial MPS/MF) para auditoria/TCE.</summary>
    public string BaseLegal { get; private set; } = default!;

    /// <summary>Faixas progressivas ordenadas por limite (somente leitura).</summary>
    public IReadOnlyList<FaixaProgressiva> Faixas => _faixas;

    /// <summary>Cria uma tabela INSS valida para uma competencia inicial.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="vigenciaInicio">Competencia inicial de vigencia.</param>
    /// <param name="faixas">Faixas progressivas (contiguas, ordenadas, terminando no teto).</param>
    /// <param name="teto">Teto do salario de contribuicao.</param>
    /// <param name="baseLegal">Fundamento legal.</param>
    /// <returns>Nova <see cref="TabelaInss"/>.</returns>
    /// <exception cref="ArgumentException">Se faixas/base legal forem invalidas.</exception>
    /// <exception cref="ArgumentNullException">Se a competencia/faixas forem nulas.</exception>
    public static TabelaInss Criar(
        Guid tenantId,
        Competencia vigenciaInicio,
        IReadOnlyList<FaixaProgressiva> faixas,
        decimal teto,
        string baseLegal)
    {
        ArgumentNullException.ThrowIfNull(vigenciaInicio);
        ArgumentNullException.ThrowIfNull(faixas);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseLegal);
        if (faixas.Count == 0)
        {
            throw new ArgumentException("Tabela INSS exige ao menos uma faixa.", nameof(faixas));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(teto);
        GarantirFaixasContiguas(faixas, teto);

        return new TabelaInss(
            TabelaInssId.New(),
            tenantId,
            vigenciaInicio,
            faixas,
            teto,
            baseLegal.Trim());
    }

    /// <summary>
    /// Calcula a contribuicao do INSS (RGPS) sobre a base, de forma progressiva e cumulativa
    /// (cada faixa incide so sobre a parcela dentro dela), respeitando o teto. Deterministico.
    /// </summary>
    /// <param name="baseContribuicao">Soma das rubricas com incidencia de INSS.</param>
    /// <returns>Valor do desconto de INSS (2 casas), nao-negativo.</returns>
    public decimal CalcularContribuicao(decimal baseContribuicao)
    {
        if (baseContribuicao <= 0m)
        {
            return 0m;
        }

        var baseLimitada = baseContribuicao > Teto ? Teto : baseContribuicao;
        var total = 0m;
        foreach (var faixa in _faixas)
        {
            total += faixa.Contribuicao(baseLimitada);
        }

        return decimal.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    private static void GarantirFaixasContiguas(IReadOnlyList<FaixaProgressiva> faixas, decimal teto)
    {
        // A primeira faixa parte de zero; cada faixa comeca onde a anterior termina; a ultima fecha no teto.
        if (faixas[0].LimiteInferior != 0m)
        {
            throw new ArgumentException("A primeira faixa do INSS deve iniciar em zero.", nameof(faixas));
        }

        for (var i = 1; i < faixas.Count; i++)
        {
            if (faixas[i].LimiteInferior != faixas[i - 1].LimiteSuperior)
            {
                throw new ArgumentException("As faixas do INSS devem ser contiguas (sem lacunas/sobreposicoes).", nameof(faixas));
            }
        }

        if (faixas[^1].LimiteSuperior != teto)
        {
            throw new ArgumentException("A ultima faixa do INSS deve coincidir com o teto.", nameof(faixas));
        }
    }
}
