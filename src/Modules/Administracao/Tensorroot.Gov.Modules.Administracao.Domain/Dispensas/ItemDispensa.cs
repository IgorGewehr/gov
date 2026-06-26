using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;

/// <summary>Identificador forte de um <see cref="ItemDispensa"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemDispensaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemDispensaId"/>.</returns>
    public static ItemDispensaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Item disputado na dispensa eletronica (objeto do aviso de contratacao direta). Cada item carrega
/// quantidade e valor unitario estimado/orcado; o valor total estimado e quantidade x unitario.
/// </summary>
public sealed class ItemDispensa : Entity<ItemDispensaId>
{
    private ItemDispensa()
    {
    }

    private ItemDispensa(
        ItemDispensaId id,
        int numero,
        Guid? itemCatalogoId,
        string descricao,
        decimal quantidade,
        ValorMonetario valorUnitarioEstimado)
        : base(id)
    {
        Numero = numero;
        ItemCatalogoId = itemCatalogoId;
        Descricao = descricao;
        Quantidade = quantidade;
        ValorUnitarioEstimado = valorUnitarioEstimado;
    }

    /// <summary>Numero do item no procedimento.</summary>
    public int Numero { get; private set; }

    /// <summary>Referencia ao item de catalogo (CATMAT/CATSER), quando padronizado.</summary>
    public Guid? ItemCatalogoId { get; private set; }

    /// <summary>Descricao do objeto do item.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Quantidade demandada.</summary>
    public decimal Quantidade { get; private set; }

    /// <summary>Valor unitario estimado/orcado.</summary>
    public ValorMonetario ValorUnitarioEstimado { get; private set; } = default!;

    /// <summary>Valor total estimado do item (quantidade x valor unitario).</summary>
    public ValorMonetario ValorTotalEstimado => ValorUnitarioEstimado.Multiplicar(Quantidade);

    /// <summary>Cria um novo item de dispensa.</summary>
    /// <param name="numero">Numero do item (maior que zero).</param>
    /// <param name="itemCatalogoId">Referencia ao catalogo (opcional).</param>
    /// <param name="descricao">Descricao do objeto.</param>
    /// <param name="quantidade">Quantidade demandada (maior que zero).</param>
    /// <param name="valorUnitarioEstimado">Valor unitario estimado/orcado.</param>
    /// <returns>Novo <see cref="ItemDispensa"/>.</returns>
    /// <exception cref="ArgumentException">Se a descricao for vazia.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o numero ou a quantidade nao forem positivos.</exception>
    public static ItemDispensa Criar(
        int numero,
        Guid? itemCatalogoId,
        string descricao,
        decimal quantidade,
        ValorMonetario valorUnitarioEstimado)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentNullException.ThrowIfNull(valorUnitarioEstimado);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numero);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        return new ItemDispensa(ItemDispensaId.New(), numero, itemCatalogoId, descricao, quantidade, valorUnitarioEstimado);
    }
}
