using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;

/// <summary>Identificador forte de um <see cref="ItemDespesaCamara"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemDespesaCamaraId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemDespesaCamaraId"/>.</returns>
    public static ItemDespesaCamaraId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Parcela DISCRIMINADA da despesa realizada do Poder Legislativo no exercicio, classificada por
/// <see cref="NaturezaDespesaCamara"/>. Entidade-filha (owned) da <see cref="ApuracaoArt29A"/> que permite
/// aplicar a regra temporal da EC 109/2021 (inativos/pensionistas) sem perder a rastreabilidade do que
/// compoe o teto e o subteto de folha.
/// </summary>
public sealed class ItemDespesaCamara : Entity<ItemDespesaCamaraId>
{
    private ItemDespesaCamara()
    {
    }

    private ItemDespesaCamara(ItemDespesaCamaraId id, NaturezaDespesaCamara natureza, decimal valor, string? descricao)
        : base(id)
    {
        Natureza = natureza;
        Valor = valor;
        Descricao = descricao;
    }

    /// <summary>Natureza da despesa (pessoal ativo, inativos/pensionistas, outras).</summary>
    public NaturezaDespesaCamara Natureza { get; private set; }

    /// <summary>Valor realizado da parcela (&gt;= 0).</summary>
    public decimal Valor { get; private set; }

    /// <summary>Descricao livre (opcional) — ex.: "Folha vereadores", "Custeio administrativo".</summary>
    public string? Descricao { get; private set; }

    /// <summary>Indica se a parcela e folha de pessoal (entra no subteto §1: ativos + inativos/pensionistas).</summary>
    public bool EhFolha => Natureza is NaturezaDespesaCamara.PessoalAtivo or NaturezaDespesaCamara.InativosPensionistas;

    /// <summary>Cria uma parcela de despesa validada.</summary>
    /// <param name="natureza">Natureza da despesa.</param>
    /// <param name="valor">Valor realizado (&gt;= 0).</param>
    /// <param name="descricao">Descricao (opcional).</param>
    /// <returns>Nova parcela.</returns>
    /// <exception cref="ArgumentException">Se a natureza for invalida.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor for negativo.</exception>
    public static ItemDespesaCamara De(NaturezaDespesaCamara natureza, decimal valor, string? descricao = null)
    {
        if (!Enum.IsDefined(natureza))
        {
            throw new ArgumentException("Natureza de despesa invalida.", nameof(natureza));
        }

        if (valor < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "Valor da despesa nao pode ser negativo.");
        }

        var descricaoLimpa = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        return new ItemDespesaCamara(ItemDespesaCamaraId.New(), natureza, valor, descricaoLimpa);
    }
}
