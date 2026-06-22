namespace Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

/// <summary>Identificador forte do agregado <see cref="SolicitacaoRegulacao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct SolicitacaoRegulacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="SolicitacaoRegulacaoId"/>.</returns>
    public static SolicitacaoRegulacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
