namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;

/// <summary>Identificador forte do agregado <see cref="LeiOrcamentariaAnual"/> (LOA).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LoaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LoaId"/>.</returns>
    public static LoaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de uma <see cref="ReceitaPrevista"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ReceitaPrevistaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ReceitaPrevistaId"/>.</returns>
    public static ReceitaPrevistaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de um <see cref="ItemDespesaFixada"/> (linha do QDD).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemDespesaFixadaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemDespesaFixadaId"/>.</returns>
    public static ItemDespesaFixadaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
