namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Creditos;

/// <summary>Identificador forte do agregado <see cref="CreditoAdicional"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CreditoAdicionalId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CreditoAdicionalId"/>.</returns>
    public static CreditoAdicionalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
