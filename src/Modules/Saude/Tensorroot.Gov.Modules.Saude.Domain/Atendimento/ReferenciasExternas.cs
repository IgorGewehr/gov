namespace Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

/// <summary>
/// Identificador forte do paciente referenciado pelo atendimento (referencia cross-aggregate
/// por Id, sem navegacao). O agregado Paciente reside no mesmo Bounded Context Saude.
/// </summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PacienteId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PacienteId"/>.</returns>
    public static PacienteId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Identificador forte do estabelecimento (CNES) referenciado pelo atendimento
/// (referencia cross-aggregate por Id, sem navegacao).
/// </summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EstabelecimentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EstabelecimentoId"/>.</returns>
    public static EstabelecimentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Identificador forte do profissional (CBO) responsavel pelo atendimento
/// (referencia cross-aggregate por Id, sem navegacao).
/// </summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ProfissionalId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ProfissionalId"/>.</returns>
    public static ProfissionalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
