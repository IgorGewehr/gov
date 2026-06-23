namespace Tensorroot.Gov.Modules.Saude.Domain.Farmacia;

/// <summary>Identificador forte do agregado <see cref="Medicamento"/> (catalogo).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MedicamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MedicamentoId"/>.</returns>
    public static MedicamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte do agregado <see cref="EstoqueMedicamento"/> (saldo por estabelecimento+medicamento).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EstoqueMedicamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EstoqueMedicamentoId"/>.</returns>
    public static EstoqueMedicamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte da entidade filha <see cref="LoteMedicamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LoteMedicamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LoteMedicamentoId"/>.</returns>
    public static LoteMedicamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte do agregado <see cref="Dispensacao"/> (entrega ao paciente).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DispensacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DispensacaoId"/>.</returns>
    public static DispensacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte da entidade filha <see cref="ItemDispensado"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemDispensadoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemDispensadoId"/>.</returns>
    public static ItemDispensadoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
