namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;

/// <summary>Identificador forte do agregado <see cref="PlanoPlurianual"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PpaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PpaId"/>.</returns>
    public static PpaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de um <see cref="Programa"/> (entidade-filha do PPA).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ProgramaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ProgramaId"/>.</returns>
    public static ProgramaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de uma <see cref="AcaoPpa"/> (entidade-filha do PPA).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AcaoPpaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AcaoPpaId"/>.</returns>
    public static AcaoPpaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de uma <see cref="MetaAcao"/> (entidade-filha do PPA).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MetaAcaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MetaAcaoId"/>.</returns>
    public static MetaAcaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
