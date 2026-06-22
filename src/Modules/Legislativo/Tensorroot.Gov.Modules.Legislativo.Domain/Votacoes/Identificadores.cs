namespace Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

/// <summary>Identificador forte do agregado <see cref="Votacao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct VotacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="VotacaoId"/>.</returns>
    public static VotacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Identificador forte de um <see cref="Voto"/>. Origem do painel eletronico e
/// chave de idempotencia (I-3): o mesmo <see cref="VotoId"/> gera apenas um voto.
/// </summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct VotoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="VotoId"/>.</returns>
    public static VotoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
