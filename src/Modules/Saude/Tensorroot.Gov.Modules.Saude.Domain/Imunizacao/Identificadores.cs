namespace Tensorroot.Gov.Modules.Saude.Domain.Imunizacao;

/// <summary>Identificador forte do agregado <see cref="Imunobiologico"/> (catalogo de vacinas).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ImunobiologicoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ImunobiologicoId"/>.</returns>
    public static ImunobiologicoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte do agregado <see cref="CarteiraVacinacao"/> (1-1 por paciente).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CarteiraVacinacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CarteiraVacinacaoId"/>.</returns>
    public static CarteiraVacinacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte da entidade filha <see cref="DoseAplicada"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DoseAplicadaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DoseAplicadaId"/>.</returns>
    public static DoseAplicadaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
