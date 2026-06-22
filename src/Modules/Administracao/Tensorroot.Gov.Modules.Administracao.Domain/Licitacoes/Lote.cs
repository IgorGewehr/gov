using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

/// <summary>Identificador forte de um <see cref="Lote"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LoteId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LoteId"/>.</returns>
    public static LoteId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Item ou agrupamento de itens disputado isoladamente no certame.</summary>
public sealed class Lote : Entity<LoteId>
{
    private Lote()
    {
    }

    private Lote(LoteId id, int numero, string descricao, ValorMonetario valorEstimado)
        : base(id)
    {
        Numero = numero;
        Descricao = descricao;
        ValorEstimado = valorEstimado;
    }

    /// <summary>Numero do lote no certame.</summary>
    public int Numero { get; private set; }

    /// <summary>Descricao do objeto do lote.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Valor estimado/orcado do lote.</summary>
    public ValorMonetario ValorEstimado { get; private set; } = default!;

    /// <summary>Cria um novo lote.</summary>
    /// <param name="numero">Numero do lote (maior que zero).</param>
    /// <param name="descricao">Descricao do objeto.</param>
    /// <param name="valorEstimado">Valor estimado/orcado.</param>
    /// <returns>Novo <see cref="Lote"/>.</returns>
    /// <exception cref="ArgumentException">Se a descricao for vazia.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o numero nao for positivo.</exception>
    public static Lote Criar(int numero, string descricao, ValorMonetario valorEstimado)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        ArgumentNullException.ThrowIfNull(valorEstimado);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numero);
        return new Lote(LoteId.New(), numero, descricao, valorEstimado);
    }
}
