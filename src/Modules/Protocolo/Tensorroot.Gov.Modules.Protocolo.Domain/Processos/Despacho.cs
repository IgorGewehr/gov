using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Protocolo.Domain.Processos;

/// <summary>Identificador forte de um <see cref="Despacho"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DespachoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DespachoId"/>.</returns>
    public static DespachoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Manifestacao/decisao de autoridade no processo (entidade interna, append-only —
/// compoe a trilha documental e nunca e excluida ou editada).
/// </summary>
public sealed class Despacho : Entity<DespachoId>
{
    private Despacho()
    {
    }

    private Despacho(DespachoId id, string texto, Guid autoridadeId, DateOnly dataDespacho)
        : base(id)
    {
        Texto = texto;
        AutoridadeId = autoridadeId;
        DataDespacho = dataDespacho;
    }

    /// <summary>Conteudo (texto) do despacho.</summary>
    public string Texto { get; private set; } = default!;

    /// <summary>Autoridade que proferiu o despacho.</summary>
    public Guid AutoridadeId { get; private set; }

    /// <summary>Data do despacho.</summary>
    public DateOnly DataDespacho { get; private set; }

    /// <summary>Registra um novo despacho no processo.</summary>
    /// <param name="texto">Conteudo do despacho.</param>
    /// <param name="autoridadeId">Autoridade que profere o despacho.</param>
    /// <param name="dataDespacho">Data do despacho.</param>
    /// <returns>Novo <see cref="Despacho"/>.</returns>
    /// <exception cref="ArgumentException">Se o texto for vazio.</exception>
    public static Despacho Registrar(string texto, Guid autoridadeId, DateOnly dataDespacho)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);
        return new Despacho(DespachoId.New(), texto.Trim(), autoridadeId, dataDespacho);
    }
}
