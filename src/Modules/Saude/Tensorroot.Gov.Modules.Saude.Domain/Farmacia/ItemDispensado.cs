using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Farmacia;

/// <summary>
/// Item efetivamente entregue ao paciente numa <see cref="Dispensacao"/>: medicamento, quantidade,
/// posologia orientada e o rastro dos lotes baixados (FEFO). Entidade filha — nasce ja com a baixa
/// computada pela raiz (a integridade do saldo e responsabilidade do <see cref="EstoqueMedicamento"/>).
/// </summary>
public sealed class ItemDispensado : Entity<ItemDispensadoId>
{
    private readonly List<BaixaLote> _baixas = [];

    private ItemDispensado()
    {
    }

    private ItemDispensado(
        ItemDispensadoId id,
        MedicamentoId medicamentoId,
        decimal quantidade,
        string posologia,
        IEnumerable<BaixaLote> baixas)
        : base(id)
    {
        MedicamentoId = medicamentoId;
        Quantidade = quantidade;
        Posologia = posologia;
        _baixas.AddRange(baixas);
    }

    /// <summary>Medicamento dispensado (referencia por Id ao catalogo).</summary>
    public MedicamentoId MedicamentoId { get; private set; }

    /// <summary>Quantidade entregue (na unidade do medicamento).</summary>
    public decimal Quantidade { get; private set; }

    /// <summary>Posologia/orientacao de uso registrada na entrega.</summary>
    public string Posologia { get; private set; } = default!;

    /// <summary>Rastro dos lotes baixados para atender este item (rastreabilidade SNGPC).</summary>
    public IReadOnlyCollection<BaixaLote> Baixas => _baixas;

    /// <summary>Cria o item dispensado a partir da baixa ja realizada no estoque.</summary>
    /// <param name="medicamentoId">Medicamento dispensado.</param>
    /// <param name="quantidade">Quantidade entregue (> 0).</param>
    /// <param name="posologia">Posologia orientada.</param>
    /// <param name="baixas">Rastro das baixas por lote.</param>
    /// <returns>Novo <see cref="ItemDispensado"/>.</returns>
    /// <exception cref="ArgumentException">Se a posologia for vazia.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade nao for positiva.</exception>
    internal static ItemDispensado Criar(
        MedicamentoId medicamentoId,
        decimal quantidade,
        string posologia,
        IReadOnlyList<BaixaLote> baixas)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(posologia);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(quantidade, 0m);
        ArgumentNullException.ThrowIfNull(baixas);
        return new ItemDispensado(ItemDispensadoId.New(), medicamentoId, quantidade, posologia.Trim(), baixas);
    }
}
