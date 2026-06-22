using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Pgv;

/// <summary>Identificador forte da entidade <see cref="FatorPgv"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct FatorPgvId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="FatorPgvId"/>.</returns>
    public static FatorPgvId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Fator de correção do valor venal (padrão construtivo, depreciação por idade, uso), pertencente a
/// uma <see cref="PlantaValores"/>. Multiplicadores definidos por lei municipal (PGV) — nunca hardcoded.
/// </summary>
public sealed class FatorPgv : Entity<FatorPgvId>
{
    private FatorPgv()
    {
    }

    private FatorPgv(FatorPgvId id, PlantaValoresId plantaId, TipoFatorPgv tipo, string chave, decimal multiplicador)
        : base(id)
    {
        PlantaValoresId = plantaId;
        Tipo = tipo;
        Chave = chave;
        Multiplicador = multiplicador;
    }

    /// <summary>Planta (PGV) à qual pertence.</summary>
    public PlantaValoresId PlantaValoresId { get; private set; }

    /// <summary>Categoria do fator.</summary>
    public TipoFatorPgv Tipo { get; private set; }

    /// <summary>Chave do fator (ex.: código de padrão, faixa de idade, uso).</summary>
    public string Chave { get; private set; } = default!;

    /// <summary>Multiplicador a aplicar (ex.: 1.20 = +20%).</summary>
    public decimal Multiplicador { get; private set; }

    /// <summary>Cria um fator de correção.</summary>
    /// <param name="plantaId">Planta dona.</param>
    /// <param name="tipo">Categoria.</param>
    /// <param name="chave">Chave.</param>
    /// <param name="multiplicador">Multiplicador (&gt; 0).</param>
    /// <returns>Novo <see cref="FatorPgv"/>.</returns>
    public static FatorPgv Criar(PlantaValoresId plantaId, TipoFatorPgv tipo, string chave, decimal multiplicador)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chave);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(multiplicador);
        return new FatorPgv(FatorPgvId.New(), plantaId, tipo, chave, multiplicador);
    }
}
