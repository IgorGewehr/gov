using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;

/// <summary>Identificador forte de uma <see cref="Adesao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AdesaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AdesaoId"/>.</returns>
    public static AdesaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Adesao (carona) a uma Ata de Registro de Precos por orgao nao participante (art. 86, Lei 14.133/2021),
/// registrando o orgao aderente, o item e a quantidade aderida. Entidade filha da <see cref="Ata"/>.
/// </summary>
public sealed class Adesao : Entity<AdesaoId>
{
    private Adesao()
    {
    }

    private Adesao(AdesaoId id, ItemCatalogoId itemCatalogoId, string orgaoAderente, decimal quantidade, DateOnly data)
        : base(id)
    {
        ItemCatalogoId = itemCatalogoId;
        OrgaoAderente = orgaoAderente;
        Quantidade = quantidade;
        Data = data;
    }

    /// <summary>Item de catalogo objeto da adesao.</summary>
    public ItemCatalogoId ItemCatalogoId { get; private set; }

    /// <summary>Identificacao do orgao/entidade aderente (carona).</summary>
    public string OrgaoAderente { get; private set; } = default!;

    /// <summary>Quantidade aderida.</summary>
    public decimal Quantidade { get; private set; }

    /// <summary>Data da adesao.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Registra uma adesao (carona).</summary>
    /// <param name="itemCatalogoId">Item objeto da adesao.</param>
    /// <param name="orgaoAderente">Orgao aderente (obrigatorio).</param>
    /// <param name="quantidade">Quantidade aderida (positiva).</param>
    /// <param name="data">Data da adesao.</param>
    /// <returns>Nova <see cref="Adesao"/>.</returns>
    /// <exception cref="ArgumentException">Se o orgao aderente for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade nao for positiva.</exception>
    public static Adesao Registrar(ItemCatalogoId itemCatalogoId, string orgaoAderente, decimal quantidade, DateOnly data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orgaoAderente);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        return new Adesao(AdesaoId.New(), itemCatalogoId, orgaoAderente.Trim(), quantidade, data);
    }
}
