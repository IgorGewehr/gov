namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;

/// <summary>Identificador forte do agregado <see cref="LeiDiretrizes"/> (LDO).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LdoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LdoId"/>.</returns>
    public static LdoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de uma <see cref="PrioridadeLdo"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PrioridadeLdoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PrioridadeLdoId"/>.</returns>
    public static PrioridadeLdoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de uma <see cref="MetaFiscal"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MetaFiscalId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MetaFiscalId"/>.</returns>
    public static MetaFiscalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de um <see cref="AnexoLdo"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AnexoLdoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AnexoLdoId"/>.</returns>
    public static AnexoLdoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
