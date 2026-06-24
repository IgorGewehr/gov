using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Pca;

/// <summary>Identificador forte de um <see cref="ItemPca"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemPcaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemPcaId"/>.</returns>
    public static ItemPcaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Item do Plano de Contratacoes Anual: contratacao pretendida no exercicio, vinculada a um item do
/// catalogo, com quantidade, valor estimado e trimestre desejado de contratacao (art. 12, VII e
/// Dec. 11.246/2022). Entidade filha do <see cref="PlanoContratacoes"/>.
/// </summary>
public sealed class ItemPca : Entity<ItemPcaId>
{
    private ItemPca()
    {
    }

    private ItemPca(
        ItemPcaId id,
        ItemCatalogoId itemCatalogoId,
        decimal quantidade,
        ValorMonetario valorEstimado,
        int trimestreDesejado,
        string? justificativa)
        : base(id)
    {
        ItemCatalogoId = itemCatalogoId;
        Quantidade = quantidade;
        ValorEstimado = valorEstimado;
        TrimestreDesejado = trimestreDesejado;
        Justificativa = justificativa;
    }

    /// <summary>Item de catalogo a contratar.</summary>
    public ItemCatalogoId ItemCatalogoId { get; private set; }

    /// <summary>Quantidade pretendida.</summary>
    public decimal Quantidade { get; private set; }

    /// <summary>Valor estimado total da contratacao pretendida.</summary>
    public ValorMonetario ValorEstimado { get; private set; } = default!;

    /// <summary>Trimestre desejado para a contratacao (1 a 4).</summary>
    public int TrimestreDesejado { get; private set; }

    /// <summary>Justificativa da necessidade (opcional).</summary>
    public string? Justificativa { get; private set; }

    /// <summary>Cria um item do PCA.</summary>
    /// <param name="itemCatalogoId">Item de catalogo a contratar.</param>
    /// <param name="quantidade">Quantidade pretendida (positiva).</param>
    /// <param name="valorEstimado">Valor estimado total.</param>
    /// <param name="trimestreDesejado">Trimestre desejado (1 a 4).</param>
    /// <param name="justificativa">Justificativa (opcional).</param>
    /// <returns>Novo <see cref="ItemPca"/>.</returns>
    /// <exception cref="ArgumentNullException">Se o valor estimado for nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade nao for positiva ou o trimestre fora de 1..4.</exception>
    public static ItemPca Criar(
        ItemCatalogoId itemCatalogoId,
        decimal quantidade,
        ValorMonetario valorEstimado,
        int trimestreDesejado,
        string? justificativa)
    {
        ArgumentNullException.ThrowIfNull(valorEstimado);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        if (trimestreDesejado is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(trimestreDesejado), "Trimestre desejado deve estar entre 1 e 4.");
        }

        return new ItemPca(
            ItemPcaId.New(),
            itemCatalogoId,
            quantidade,
            valorEstimado,
            trimestreDesejado,
            string.IsNullOrWhiteSpace(justificativa) ? null : justificativa.Trim());
    }
}
