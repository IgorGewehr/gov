using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Taxas;

/// <summary>Identificador forte da entidade <see cref="FaixaTaxa"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct FaixaTaxaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="FaixaTaxaId"/>.</returns>
    public static FaixaTaxaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Faixa da quantidade-base de uma <see cref="TabelaTaxa"/> no modo PorFaixa: um intervalo
/// [<see cref="LimiteInferior"/>, <see cref="LimiteSuperior"/>] (superior nulo = sem teto) com o valor
/// fixo da taxa naquela faixa. Entidade filha da tabela (lei municipal). Ver M6-DESIGN §3.2.
/// </summary>
public sealed class FaixaTaxa : Entity<FaixaTaxaId>
{
    private FaixaTaxa()
    {
    }

    private FaixaTaxa(FaixaTaxaId id, TabelaTaxaId tabelaTaxaId, decimal limiteInferior, decimal? limiteSuperior, ValorMonetario valor)
        : base(id)
    {
        TabelaTaxaId = tabelaTaxaId;
        LimiteInferior = limiteInferior;
        LimiteSuperior = limiteSuperior;
        Valor = valor;
    }

    /// <summary>Tabela de taxa dona da faixa.</summary>
    public TabelaTaxaId TabelaTaxaId { get; private set; }

    /// <summary>Limite inferior (inclusivo) da quantidade-base.</summary>
    public decimal LimiteInferior { get; private set; }

    /// <summary>Limite superior (inclusivo) da quantidade-base; nulo = sem teto.</summary>
    public decimal? LimiteSuperior { get; private set; }

    /// <summary>Valor da taxa nesta faixa.</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Cria uma faixa da tabela de taxa.</summary>
    /// <param name="tabelaTaxaId">Tabela dona.</param>
    /// <param name="limiteInferior">Limite inferior (inclusivo).</param>
    /// <param name="limiteSuperior">Limite superior (inclusivo); nulo = sem teto.</param>
    /// <param name="valor">Valor da taxa na faixa.</param>
    /// <returns>Nova <see cref="FaixaTaxa"/>.</returns>
    public static FaixaTaxa Criar(TabelaTaxaId tabelaTaxaId, decimal limiteInferior, decimal? limiteSuperior, ValorMonetario valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        return new FaixaTaxa(FaixaTaxaId.New(), tabelaTaxaId, limiteInferior, limiteSuperior, valor);
    }

    /// <summary>Indica se a quantidade-base está contida nesta faixa.</summary>
    /// <param name="quantidadeBase">Quantidade-base a testar.</param>
    /// <returns>Verdadeiro se contida.</returns>
    public bool Contem(decimal quantidadeBase)
        => quantidadeBase >= LimiteInferior && (LimiteSuperior is null || quantidadeBase <= LimiteSuperior.Value);
}
