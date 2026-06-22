using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

/// <summary>Identificador forte de um <see cref="ItemOrdemDoDia"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemId"/>.</returns>
    public static ItemId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Item da Ordem do Dia: proposicao pautada para deliberacao numa sessao. Entidade do
/// agregado <see cref="Sessao"/>, ordenada por numero de pauta.
/// </summary>
public sealed class ItemOrdemDoDia : Entity<ItemId>
{
    private ItemOrdemDoDia()
    {
    }

    private ItemOrdemDoDia(ItemId id, ProposicaoId proposicaoId, int ordem)
        : base(id)
    {
        ProposicaoId = proposicaoId;
        Ordem = ordem;
    }

    /// <summary>Proposicao pautada para deliberacao.</summary>
    public ProposicaoId ProposicaoId { get; private set; }

    /// <summary>Numero de ordem (posicao) na pauta.</summary>
    public int Ordem { get; private set; }

    /// <summary>Inclui uma proposicao na Ordem do Dia.</summary>
    /// <param name="proposicaoId">Proposicao a pautar.</param>
    /// <param name="ordem">Posicao na pauta (maior ou igual a 1).</param>
    /// <returns>Novo <see cref="ItemOrdemDoDia"/>.</returns>
    public static ItemOrdemDoDia Incluir(ProposicaoId proposicaoId, int ordem)
        => new(ItemId.New(), proposicaoId, ordem);
}
