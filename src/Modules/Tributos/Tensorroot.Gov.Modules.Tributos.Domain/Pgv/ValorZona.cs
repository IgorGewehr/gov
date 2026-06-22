using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Pgv;

/// <summary>Identificador forte da entidade <see cref="ValorZona"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ValorZonaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ValorZonaId"/>.</returns>
    public static ValorZonaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Valor unitário do m² de terreno (VUT) e de construção (VUC) de uma zona fiscal, pertencente a
/// uma <see cref="PlantaValores"/>. Valores definidos por lei municipal (PGV) — nunca hardcoded.
/// </summary>
public sealed class ValorZona : Entity<ValorZonaId>
{
    private ValorZona()
    {
    }

    private ValorZona(ValorZonaId id, PlantaValoresId plantaId, string zonaFiscal, decimal valorM2Terreno, decimal valorM2Construcao)
        : base(id)
    {
        PlantaValoresId = plantaId;
        ZonaFiscal = zonaFiscal;
        ValorM2Terreno = valorM2Terreno;
        ValorM2Construcao = valorM2Construcao;
    }

    /// <summary>Planta (PGV) à qual pertence.</summary>
    public PlantaValoresId PlantaValoresId { get; private set; }

    /// <summary>Código da zona fiscal.</summary>
    public string ZonaFiscal { get; private set; } = default!;

    /// <summary>Valor unitário do m² de terreno (VUT) em R$.</summary>
    public decimal ValorM2Terreno { get; private set; }

    /// <summary>Valor unitário do m² de construção (VUC) em R$.</summary>
    public decimal ValorM2Construcao { get; private set; }

    /// <summary>Cria o valor unitário de uma zona.</summary>
    /// <param name="plantaId">Planta dona.</param>
    /// <param name="zonaFiscal">Código da zona.</param>
    /// <param name="valorM2Terreno">VUT em R$ (≥ 0).</param>
    /// <param name="valorM2Construcao">VUC em R$ (≥ 0).</param>
    /// <returns>Nova <see cref="ValorZona"/>.</returns>
    public static ValorZona Criar(PlantaValoresId plantaId, string zonaFiscal, decimal valorM2Terreno, decimal valorM2Construcao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zonaFiscal);
        ArgumentOutOfRangeException.ThrowIfNegative(valorM2Terreno);
        ArgumentOutOfRangeException.ThrowIfNegative(valorM2Construcao);
        return new ValorZona(
            ValorZonaId.New(),
            plantaId,
            zonaFiscal,
            decimal.Round(valorM2Terreno, 2, MidpointRounding.AwayFromZero),
            decimal.Round(valorM2Construcao, 2, MidpointRounding.AwayFromZero));
    }
}
