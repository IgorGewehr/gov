using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Iss;

/// <summary>Identificador forte da entidade <see cref="ItemAliquotaIss"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemAliquotaIssId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemAliquotaIssId"/>.</returns>
    public static ItemAliquotaIssId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Alíquota do ISS por item da lista de serviços da LC 116/2003 (LEI MUNICIPAL), com indicação de
/// retenção obrigatória e responsabilidade por substituição tributária — todos parametrizáveis por
/// tenant. Entidade-filha da <see cref="TabelaAliquotaIss"/>. // TODO(validar-oficial): alíquotas,
/// hipóteses de retenção e lista de substitutos conforme o CTM de Maximiliano de Almeida/RS (M6-DESIGN §2.2).
/// </summary>
public sealed class ItemAliquotaIss : Entity<ItemAliquotaIssId>
{
    private ItemAliquotaIss()
    {
    }

    private ItemAliquotaIss(
        ItemAliquotaIssId id,
        TabelaAliquotaIssId tabelaId,
        string itemListaServico,
        decimal aliquotaPercentual,
        bool retencaoObrigatoria,
        bool substituicaoTributaria)
        : base(id)
    {
        TabelaAliquotaIssId = tabelaId;
        ItemListaServico = itemListaServico;
        AliquotaPercentual = aliquotaPercentual;
        RetencaoObrigatoria = retencaoObrigatoria;
        SubstituicaoTributaria = substituicaoTributaria;
    }

    /// <summary>Tabela à qual o item pertence.</summary>
    public TabelaAliquotaIssId TabelaAliquotaIssId { get; private set; }

    /// <summary>Item da lista de serviços LC 116/2003 (ex.: "7.02").</summary>
    public string ItemListaServico { get; private set; } = default!;

    /// <summary>Alíquota em % (ex.: 2.0 = 2%). Mínimo 2% e máximo 5% conforme normas gerais — validado por lei municipal.</summary>
    public decimal AliquotaPercentual { get; private set; }

    /// <summary>
    /// Indica que, para este item, o ISS é retido na fonte pelo tomador (LC 116 art. 6º §2º II) —
    /// hipótese definida pela lei municipal, complementar ao indicador vindo do XML.
    /// </summary>
    public bool RetencaoObrigatoria { get; private set; }

    /// <summary>
    /// Indica que, para este item, há substituição tributária (LC 116 art. 6º caput) — faculdade do
    /// município, depende de lei.
    /// </summary>
    public bool SubstituicaoTributaria { get; private set; }

    /// <summary>Cria um item de alíquota do ISS.</summary>
    /// <param name="tabelaId">Tabela proprietária.</param>
    /// <param name="itemListaServico">Item da lista LC 116.</param>
    /// <param name="aliquotaPercentual">Alíquota em % (0 a 100).</param>
    /// <param name="retencaoObrigatoria">Retenção na fonte obrigatória por lei municipal.</param>
    /// <param name="substituicaoTributaria">Substituição tributária por lei municipal.</param>
    /// <returns>Nova <see cref="ItemAliquotaIss"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se a alíquota estiver fora de [0, 100].</exception>
    public static ItemAliquotaIss Criar(
        TabelaAliquotaIssId tabelaId,
        string itemListaServico,
        decimal aliquotaPercentual,
        bool retencaoObrigatoria,
        bool substituicaoTributaria)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemListaServico);
        if (aliquotaPercentual is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(aliquotaPercentual), aliquotaPercentual, "A alíquota do ISS deve estar entre 0 e 100.");
        }

        return new ItemAliquotaIss(
            ItemAliquotaIssId.New(),
            tabelaId,
            itemListaServico.Trim(),
            aliquotaPercentual,
            retencaoObrigatoria,
            substituicaoTributaria);
    }
}
