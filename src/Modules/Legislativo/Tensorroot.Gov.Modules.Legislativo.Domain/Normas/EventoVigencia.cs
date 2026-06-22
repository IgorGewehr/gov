using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Normas;

/// <summary>Identificador forte de um <see cref="EventoVigencia"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EventoVigenciaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EventoVigenciaId"/>.</returns>
    public static EventoVigenciaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Evento da trilha imutavel (append-only) de vigencia de uma <see cref="Norma"/>: promulgacao,
/// alteracao ou revogacao, com data e eventual norma de referencia. Prova juridica/LAI (N-7).
/// </summary>
public sealed class EventoVigencia : Entity<EventoVigenciaId>
{
    private EventoVigencia()
    {
    }

    private EventoVigencia(
        EventoVigenciaId id,
        TipoEventoVigencia tipo,
        DateOnly data,
        NormaId? normaReferenciaId,
        string? observacao)
        : base(id)
    {
        Tipo = tipo;
        Data = data;
        NormaReferenciaId = normaReferenciaId;
        Observacao = observacao;
    }

    /// <summary>Tipo do evento (promulgacao/alteracao/revogacao).</summary>
    public TipoEventoVigencia Tipo { get; private set; }

    /// <summary>Data do evento.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Norma que alterou/revogou esta (quando aplicavel).</summary>
    public NormaId? NormaReferenciaId { get; private set; }

    /// <summary>Observacao livre (opcional).</summary>
    public string? Observacao { get; private set; }

    /// <summary>Cria um evento de vigencia para a trilha.</summary>
    /// <param name="tipo">Tipo do evento.</param>
    /// <param name="data">Data do evento.</param>
    /// <param name="normaReferenciaId">Norma de referencia (opcional).</param>
    /// <param name="observacao">Observacao (opcional).</param>
    /// <returns>Novo <see cref="EventoVigencia"/>.</returns>
    public static EventoVigencia Registrar(
        TipoEventoVigencia tipo,
        DateOnly data,
        NormaId? normaReferenciaId = null,
        string? observacao = null)
        => new(EventoVigenciaId.New(), tipo, data, normaReferenciaId, observacao);
}
