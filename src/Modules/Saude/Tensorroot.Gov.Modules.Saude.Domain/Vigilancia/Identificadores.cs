namespace Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

/// <summary>Identificador forte do agregado <see cref="EstabelecimentoFiscalizavel"/> (sujeito a VISA).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EstabelecimentoFiscalizavelId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EstabelecimentoFiscalizavelId"/>.</returns>
    public static EstabelecimentoFiscalizavelId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte do agregado <see cref="Inspecao"/> (vistoria sanitaria).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct InspecaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="InspecaoId"/>.</returns>
    public static InspecaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte da entidade filha <see cref="ItemInspecao"/> (item do roteiro/checklist).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemInspecaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemInspecaoId"/>.</returns>
    public static ItemInspecaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte do agregado <see cref="AutoVisa"/> (auto de infracao/intimacao).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AutoVisaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AutoVisaId"/>.</returns>
    public static AutoVisaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte do agregado <see cref="LicencaSanitaria"/> (alvara sanitario).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LicencaSanitariaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LicencaSanitariaId"/>.</returns>
    public static LicencaSanitariaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
