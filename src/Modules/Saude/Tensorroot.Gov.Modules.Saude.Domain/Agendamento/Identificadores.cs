namespace Tensorroot.Gov.Modules.Saude.Domain.Agendamento;

/// <summary>Identificador forte do agregado <see cref="AgendaProfissional"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AgendaProfissionalId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AgendaProfissionalId"/>.</returns>
    public static AgendaProfissionalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte da <see cref="Vaga"/> (slot owned da agenda; referenciado pelo agendamento por Id).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct VagaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="VagaId"/>.</returns>
    public static VagaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte do agregado <see cref="Agendamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AgendamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AgendamentoId"/>.</returns>
    public static AgendamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte do agregado <see cref="FilaEspera"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct FilaEsperaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="FilaEsperaId"/>.</returns>
    public static FilaEsperaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
