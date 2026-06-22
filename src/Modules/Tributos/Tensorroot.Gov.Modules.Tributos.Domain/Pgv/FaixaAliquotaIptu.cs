using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Pgv;

/// <summary>Identificador forte da entidade <see cref="FaixaAliquotaIptu"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct FaixaAliquotaIptuId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="FaixaAliquotaIptuId"/>.</returns>
    public static FaixaAliquotaIptuId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Faixa de progressividade da <see cref="TabelaAliquotaIptu"/>: associa um intervalo de valor venal
/// [mínimo, máximo) a uma alíquota em %. Valores definidos por lei municipal — nunca hardcoded.
/// </summary>
public sealed class FaixaAliquotaIptu : Entity<FaixaAliquotaIptuId>
{
    private FaixaAliquotaIptu()
    {
    }

    private FaixaAliquotaIptu(FaixaAliquotaIptuId id, TabelaAliquotaIptuId tabelaId, decimal minimo, decimal maximo, decimal aliquota)
        : base(id)
    {
        TabelaAliquotaIptuId = tabelaId;
        ValorVenalMinimo = minimo;
        ValorVenalMaximo = maximo;
        AliquotaPercentual = aliquota;
    }

    /// <summary>Tabela à qual pertence.</summary>
    public TabelaAliquotaIptuId TabelaAliquotaIptuId { get; private set; }

    /// <summary>Limite inferior do valor venal (inclusivo).</summary>
    public decimal ValorVenalMinimo { get; private set; }

    /// <summary>Limite superior do valor venal (exclusivo).</summary>
    public decimal ValorVenalMaximo { get; private set; }

    /// <summary>Alíquota em % (ex.: 1.0 = 1%).</summary>
    public decimal AliquotaPercentual { get; private set; }

    /// <summary>Cria uma faixa de alíquota válida.</summary>
    /// <param name="tabelaId">Tabela dona.</param>
    /// <param name="valorVenalMinimo">Limite inferior (≥ 0).</param>
    /// <param name="valorVenalMaximo">Limite superior (&gt; mínimo).</param>
    /// <param name="aliquotaPercentual">Alíquota em % (≥ 0).</param>
    /// <returns>Nova <see cref="FaixaAliquotaIptu"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se os limites/alíquota forem inválidos.</exception>
    public static FaixaAliquotaIptu Criar(TabelaAliquotaIptuId tabelaId, decimal valorVenalMinimo, decimal valorVenalMaximo, decimal aliquotaPercentual)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valorVenalMinimo);
        ArgumentOutOfRangeException.ThrowIfNegative(aliquotaPercentual);
        if (valorVenalMaximo <= valorVenalMinimo)
        {
            throw new ArgumentOutOfRangeException(nameof(valorVenalMaximo), valorVenalMaximo, "O limite superior deve ser maior que o inferior.");
        }

        return new FaixaAliquotaIptu(FaixaAliquotaIptuId.New(), tabelaId, valorVenalMinimo, valorVenalMaximo, aliquotaPercentual);
    }
}
