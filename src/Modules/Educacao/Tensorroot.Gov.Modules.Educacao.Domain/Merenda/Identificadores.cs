namespace Tensorroot.Gov.Modules.Educacao.Domain.Merenda;

/// <summary>Identificador forte do agregado <see cref="Cardapio"/> (planejamento nutricional semanal).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CardapioId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CardapioId"/>.</returns>
    public static CardapioId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte da entidade-filha <see cref="ItemCardapio"/> (genero por refeicao/dia).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemCardapioId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemCardapioId"/>.</returns>
    public static ItemCardapioId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte do agregado <see cref="DistribuicaoMerenda"/> (consumo efetivo do dia).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DistribuicaoMerendaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DistribuicaoMerendaId"/>.</returns>
    public static DistribuicaoMerendaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte da entidade-filha <see cref="ConsumoGenero"/> (baixa por genero).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ConsumoGeneroId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ConsumoGeneroId"/>.</returns>
    public static ConsumoGeneroId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
