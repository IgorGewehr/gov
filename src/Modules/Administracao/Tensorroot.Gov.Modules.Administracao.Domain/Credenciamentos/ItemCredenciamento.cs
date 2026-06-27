using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Credenciamentos;

/// <summary>Identificador forte de um <see cref="ItemCredenciamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemCredenciamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemCredenciamentoId"/>.</returns>
    public static ItemCredenciamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Objeto credenciavel do edital (servico/bem que pode ser prestado/fornecido pelos credenciados). No
/// credenciamento NAO ha disputa de preco entre interessados: a remuneracao e o PRECO FIXADO pela
/// Administracao no edital (tabela de precos previa), ao qual o interessado adere (Lei 14.133/2021,
/// art. 79). Entidade filha do agregado <see cref="Credenciamento"/>.
/// </summary>
public sealed class ItemCredenciamento : Entity<ItemCredenciamentoId>
{
    private ItemCredenciamento()
    {
    }

    private ItemCredenciamento(
        ItemCredenciamentoId id,
        int numero,
        Guid? itemCatalogoId,
        string descricao,
        string unidadeMedida,
        ValorMonetario precoFixado)
        : base(id)
    {
        Numero = numero;
        ItemCatalogoId = itemCatalogoId;
        Descricao = descricao;
        UnidadeMedida = unidadeMedida;
        PrecoFixado = precoFixado;
    }

    /// <summary>Numero sequencial do item no edital.</summary>
    public int Numero { get; private set; }

    /// <summary>Referencia ao item de catalogo (CATMAT/CATSER), quando padronizado.</summary>
    public Guid? ItemCatalogoId { get; private set; }

    /// <summary>Descricao do objeto credenciavel.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Unidade de medida/prestacao (ex.: "consulta", "exame", "km", "litro").</summary>
    public string UnidadeMedida { get; private set; } = default!;

    /// <summary>
    /// Preco unitario FIXADO pela Administracao para o item (tabela de precos do edital). No credenciamento
    /// nao ha lance/competicao por preco: o credenciado adere a este valor (art. 79).
    /// </summary>
    public ValorMonetario PrecoFixado { get; private set; } = default!;

    /// <summary>Cria um item credenciavel com preco fixado pela Administracao.</summary>
    /// <param name="numero">Numero sequencial (maior que zero).</param>
    /// <param name="itemCatalogoId">Referencia ao catalogo (opcional).</param>
    /// <param name="descricao">Descricao do objeto.</param>
    /// <param name="unidadeMedida">Unidade de prestacao/medida.</param>
    /// <param name="precoFixado">Preco unitario fixado pela Administracao.</param>
    /// <returns>Novo <see cref="ItemCredenciamento"/>.</returns>
    /// <exception cref="ArgumentException">Descricao/unidade vazias.</exception>
    /// <exception cref="ArgumentNullException">Preco nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Numero nao positivo.</exception>
    public static ItemCredenciamento Criar(
        int numero,
        Guid? itemCatalogoId,
        string descricao,
        string unidadeMedida,
        ValorMonetario precoFixado)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentException.ThrowIfNullOrWhiteSpace(unidadeMedida);
        ArgumentNullException.ThrowIfNull(precoFixado);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numero);
        return new ItemCredenciamento(ItemCredenciamentoId.New(), numero, itemCatalogoId, descricao, unidadeMedida.Trim(), precoFixado);
    }

    /// <summary>Ajusta o preco fixado do item (revisao da tabela do edital, antes de gerar efeitos a credenciados novos).</summary>
    /// <param name="novoPreco">Novo preco unitario fixado.</param>
    /// <exception cref="ArgumentNullException">Preco nulo.</exception>
    public void AjustarPreco(ValorMonetario novoPreco)
    {
        ArgumentNullException.ThrowIfNull(novoPreco);
        PrecoFixado = novoPreco;
    }
}
