using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

/// <summary>Identificador forte de uma <see cref="Prescricao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PrescricaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PrescricaoId"/>.</returns>
    public static PrescricaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Item prescrito no atendimento (medicamento/conduta), com posologia.</summary>
public sealed class Prescricao : Entity<PrescricaoId>
{
    private Prescricao()
    {
    }

    private Prescricao(PrescricaoId id, string item, string posologia, DateTimeOffset dataHora)
        : base(id)
    {
        Item = item;
        Posologia = posologia;
        DataHora = dataHora;
    }

    /// <summary>Item prescrito (medicamento/conduta).</summary>
    public string Item { get; private set; } = default!;

    /// <summary>Posologia/orientacao de uso.</summary>
    public string Posologia { get; private set; } = default!;

    /// <summary>Data/hora da prescricao.</summary>
    public DateTimeOffset DataHora { get; private set; }

    /// <summary>Registra uma nova prescricao.</summary>
    /// <param name="item">Item prescrito.</param>
    /// <param name="posologia">Posologia.</param>
    /// <param name="dataHora">Data/hora.</param>
    /// <returns>Nova <see cref="Prescricao"/>.</returns>
    /// <exception cref="ArgumentException">Se item ou posologia forem vazios.</exception>
    public static Prescricao Registrar(string item, string posologia, DateTimeOffset dataHora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(posologia);
        return new Prescricao(PrescricaoId.New(), item.Trim(), posologia.Trim(), dataHora);
    }
}
