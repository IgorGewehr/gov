using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

/// <summary>Identificador forte de uma <see cref="Reavaliacao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ReavaliacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ReavaliacaoId"/>.</returns>
    public static ReavaliacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Ajuste do valor contábil ao valor justo informado, com laudo de respaldo.</summary>
public sealed class Reavaliacao : Entity<ReavaliacaoId>
{
    private Reavaliacao()
    {
    }

    private Reavaliacao(ReavaliacaoId id, DateOnly data, decimal novoValorJusto, string laudoUri)
        : base(id)
    {
        Data = data;
        NovoValorJusto = novoValorJusto;
        LaudoUri = laudoUri;
    }

    /// <summary>Data da reavaliação.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Novo valor justo apurado.</summary>
    public decimal NovoValorJusto { get; private set; }

    /// <summary>Referência (URI) do laudo de reavaliação.</summary>
    public string LaudoUri { get; private set; } = default!;

    /// <summary>Registra uma reavaliação a valor justo.</summary>
    /// <param name="data">Data da reavaliação.</param>
    /// <param name="novoValorJusto">Novo valor justo.</param>
    /// <param name="laudoUri">Referência (URI) do laudo (obrigatório).</param>
    /// <returns>Nova <see cref="Reavaliacao"/>.</returns>
    /// <exception cref="ArgumentException">Se o laudo não for informado.</exception>
    public static Reavaliacao Registrar(DateOnly data, decimal novoValorJusto, string laudoUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(laudoUri);
        return new Reavaliacao(ReavaliacaoId.New(), data, novoValorJusto, laudoUri);
    }
}
