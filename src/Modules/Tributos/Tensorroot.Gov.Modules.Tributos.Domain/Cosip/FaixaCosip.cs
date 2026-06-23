using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Cosip;

/// <summary>Identificador forte da entidade <see cref="FaixaCosip"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct FaixaCosipId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="FaixaCosipId"/>.</returns>
    public static FaixaCosipId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Faixa de consumo (kWh) de uma classe de consumidor na <see cref="TabelaCosip"/>: intervalo
/// [<see cref="ConsumoMinimoKwh"/>, <see cref="ConsumoMaximoKwh"/>] (máximo nulo = sem teto) com o valor
/// da COSIP naquela faixa. Entidade filha da tabela (lei municipal). Ver M6-DESIGN §3.4.
/// </summary>
public sealed class FaixaCosip : Entity<FaixaCosipId>
{
    private FaixaCosip()
    {
    }

    private FaixaCosip(
        FaixaCosipId id,
        TabelaCosipId tabelaCosipId,
        ClasseConsumidorCosip classe,
        decimal consumoMinimoKwh,
        decimal? consumoMaximoKwh,
        ValorMonetario valor)
        : base(id)
    {
        TabelaCosipId = tabelaCosipId;
        Classe = classe;
        ConsumoMinimoKwh = consumoMinimoKwh;
        ConsumoMaximoKwh = consumoMaximoKwh;
        Valor = valor;
    }

    /// <summary>Tabela de COSIP dona da faixa.</summary>
    public TabelaCosipId TabelaCosipId { get; private set; }

    /// <summary>Classe de consumidor.</summary>
    public ClasseConsumidorCosip Classe { get; private set; }

    /// <summary>Consumo mínimo (inclusivo) da faixa, em kWh.</summary>
    public decimal ConsumoMinimoKwh { get; private set; }

    /// <summary>Consumo máximo (inclusivo) da faixa em kWh; nulo = sem teto.</summary>
    public decimal? ConsumoMaximoKwh { get; private set; }

    /// <summary>Valor da COSIP nesta faixa.</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Cria uma faixa de consumo da tabela de COSIP.</summary>
    /// <param name="tabelaCosipId">Tabela dona.</param>
    /// <param name="classe">Classe de consumidor.</param>
    /// <param name="consumoMinimoKwh">Consumo mínimo (inclusivo) em kWh.</param>
    /// <param name="consumoMaximoKwh">Consumo máximo (inclusivo) em kWh; nulo = sem teto.</param>
    /// <param name="valor">Valor da COSIP na faixa.</param>
    /// <returns>Nova <see cref="FaixaCosip"/>.</returns>
    public static FaixaCosip Criar(
        TabelaCosipId tabelaCosipId,
        ClasseConsumidorCosip classe,
        decimal consumoMinimoKwh,
        decimal? consumoMaximoKwh,
        ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        return new FaixaCosip(FaixaCosipId.New(), tabelaCosipId, classe, consumoMinimoKwh, consumoMaximoKwh, valor);
    }

    /// <summary>Indica se o consumo está contido nesta faixa.</summary>
    /// <param name="consumoKwh">Consumo a testar (kWh).</param>
    /// <returns>Verdadeiro se contido.</returns>
    public bool Contem(decimal consumoKwh)
        => consumoKwh >= ConsumoMinimoKwh && (ConsumoMaximoKwh is null || consumoKwh <= ConsumoMaximoKwh.Value);
}
