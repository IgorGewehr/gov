using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira;

/// <summary>Identificador forte da entidade <see cref="MovimentacaoCarreira"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MovimentacaoCarreiraId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MovimentacaoCarreiraId"/>.</returns>
    public static MovimentacaoCarreiraId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Movimentacao funcional registrada no historico de um <see cref="EnquadramentoServidor"/>: o
/// enquadramento inicial e cada progressao/promocao subsequente, com a posicao de origem e destino, o
/// criterio que a fundamentou, o vencimento resultante e o ato de pessoal (portaria) que a formalizou.
/// Imutavel apos registrada (livro-razao da vida funcional na carreira). Entidade filha — criada apenas
/// pela raiz <see cref="EnquadramentoServidor"/>.
/// </summary>
public sealed class MovimentacaoCarreira : Entity<MovimentacaoCarreiraId>
{
    private MovimentacaoCarreira()
    {
    }

    internal MovimentacaoCarreira(
        MovimentacaoCarreiraId id,
        TipoMovimentacaoCarreira tipo,
        int? classeOrigem,
        int? referenciaOrigem,
        int classeDestino,
        int referenciaDestino,
        decimal vencimentoResultante,
        CriterioProgressao? criterio,
        DateOnly dataEfeito,
        string fundamento,
        Guid? portariaId)
        : base(id)
    {
        Tipo = tipo;
        ClasseOrigem = classeOrigem;
        ReferenciaOrigem = referenciaOrigem;
        ClasseDestino = classeDestino;
        ReferenciaDestino = referenciaDestino;
        VencimentoResultante = vencimentoResultante;
        Criterio = criterio;
        DataEfeito = dataEfeito;
        Fundamento = fundamento;
        PortariaId = portariaId;
    }

    /// <summary>Natureza da movimentacao (enquadramento/progressao/promocao).</summary>
    public TipoMovimentacaoCarreira Tipo { get; private set; }

    /// <summary>Classe de origem (nula no enquadramento inicial).</summary>
    public int? ClasseOrigem { get; private set; }

    /// <summary>Referencia de origem (nula no enquadramento inicial).</summary>
    public int? ReferenciaOrigem { get; private set; }

    /// <summary>Classe de destino.</summary>
    public int ClasseDestino { get; private set; }

    /// <summary>Referencia de destino.</summary>
    public int ReferenciaDestino { get; private set; }

    /// <summary>Vencimento (BRL) resultante da nova posicao na matriz.</summary>
    public decimal VencimentoResultante { get; private set; }

    /// <summary>Criterio da progressao (nulo no enquadramento e na promocao por outros requisitos).</summary>
    public CriterioProgressao? Criterio { get; private set; }

    /// <summary>Data de efeito (vigencia) da movimentacao.</summary>
    public DateOnly DataEfeito { get; private set; }

    /// <summary>Fundamento (lei/processo/justificativa) da movimentacao.</summary>
    public string Fundamento { get; private set; } = default!;

    /// <summary>Portaria (ato de pessoal) que formalizou a movimentacao (opcional).</summary>
    public Guid? PortariaId { get; private set; }
}
