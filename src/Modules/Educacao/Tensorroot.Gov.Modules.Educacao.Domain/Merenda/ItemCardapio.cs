using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Merenda;

/// <summary>
/// Entidade-filha do <see cref="Cardapio"/>: um genero alimenticio planejado para uma refeicao em um
/// dia da semana, com a quantidade per capita (por comensal). O genero e um item do almoxarifado de
/// Patrimonio referenciado por Id (FK logica cross-module — <c>ItemEstoqueId</c> como Guid). O consumo
/// efetivo (baixa de estoque) ocorre na <see cref="DistribuicaoMerenda"/>; aqui e so planejamento.
/// </summary>
public sealed class ItemCardapio : Entity<ItemCardapioId>
{
    private ItemCardapio()
    {
    }

    private ItemCardapio(
        ItemCardapioId id,
        DiaSemanaCardapio dia,
        TipoRefeicao refeicao,
        Guid generoEstoqueId,
        decimal quantidadePerCapita,
        string unidadeMedida)
        : base(id)
    {
        Dia = dia;
        Refeicao = refeicao;
        GeneroEstoqueId = generoEstoqueId;
        QuantidadePerCapita = quantidadePerCapita;
        UnidadeMedida = unidadeMedida;
    }

    /// <summary>Dia da semana do planejamento.</summary>
    public DiaSemanaCardapio Dia { get; private set; }

    /// <summary>Tipo de refeicao planejada.</summary>
    public TipoRefeicao Refeicao { get; private set; }

    /// <summary>Genero alimenticio (FK logica ao <c>ItemEstoque</c> do almoxarifado de Patrimonio).</summary>
    public Guid GeneroEstoqueId { get; private set; }

    /// <summary>Quantidade per capita (por comensal) na unidade do genero (ex.: 0,080 kg).</summary>
    public decimal QuantidadePerCapita { get; private set; }

    /// <summary>Unidade de medida do genero (kg, L, un) — espelha o item de estoque.</summary>
    public string UnidadeMedida { get; private set; } = string.Empty;

    /// <summary>Planeja um genero por refeicao/dia, com per capita positivo (I-M2).</summary>
    /// <param name="dia">Dia da semana.</param>
    /// <param name="refeicao">Tipo de refeicao.</param>
    /// <param name="generoEstoqueId">Genero (ItemEstoque de Patrimonio) por Id.</param>
    /// <param name="quantidadePerCapita">Quantidade per capita (&gt; 0).</param>
    /// <param name="unidadeMedida">Unidade de medida do genero.</param>
    /// <returns>Novo <see cref="ItemCardapio"/>.</returns>
    /// <exception cref="ArgumentException">Se o genero for vazio ou a unidade nao informada.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o per capita nao for positivo ou enum invalido (I-M2).</exception>
    internal static ItemCardapio Planejar(
        DiaSemanaCardapio dia,
        TipoRefeicao refeicao,
        Guid generoEstoqueId,
        decimal quantidadePerCapita,
        string unidadeMedida)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unidadeMedida);
        if (generoEstoqueId == Guid.Empty)
        {
            throw new ArgumentException("Genero (ItemEstoque) obrigatorio.", nameof(generoEstoqueId));
        }

        if (!Enum.IsDefined(dia))
        {
            throw new ArgumentOutOfRangeException(nameof(dia), "Dia da semana invalido.");
        }

        if (!Enum.IsDefined(refeicao))
        {
            throw new ArgumentOutOfRangeException(nameof(refeicao), "Tipo de refeicao invalido.");
        }

        if (quantidadePerCapita <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidadePerCapita), "Per capita deve ser positivo (I-M2).");
        }

        return new ItemCardapio(ItemCardapioId.New(), dia, refeicao, generoEstoqueId, quantidadePerCapita, unidadeMedida.Trim());
    }
}
