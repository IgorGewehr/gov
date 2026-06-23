namespace Tensorroot.Gov.Modules.Educacao.Domain.Transporte;

/// <summary>Identificador forte do agregado <see cref="RotaTransporte"/> (itinerario/turno/veiculo).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RotaTransporteId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RotaTransporteId"/>.</returns>
    public static RotaTransporteId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte da entidade-filha <see cref="AlunoTransportado"/> (aluno na rota).</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AlunoTransportadoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AlunoTransportadoId"/>.</returns>
    public static AlunoTransportadoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
