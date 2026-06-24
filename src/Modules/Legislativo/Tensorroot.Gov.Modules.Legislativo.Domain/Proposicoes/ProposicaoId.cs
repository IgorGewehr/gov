namespace Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

/// <summary>Identificador forte do agregado <see cref="Proposicao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ProposicaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ProposicaoId"/>.</returns>
    public static ProposicaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
