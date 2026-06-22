using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

/// <summary>Identificador forte de uma <see cref="Emenda"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EmendaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EmendaId"/>.</returns>
    public static EmendaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Modificacao pontual a uma proposicao em curso. Entidade do agregado
/// <see cref="Proposicao"/>, vinculada por <see cref="ProposicaoId"/>.
/// </summary>
public sealed class Emenda : Entity<EmendaId>
{
    private Emenda()
    {
    }

    private Emenda(EmendaId id, ProposicaoId proposicaoId, string texto, Autoria autoria, DateOnly data)
        : base(id)
    {
        ProposicaoId = proposicaoId;
        Texto = texto;
        Autoria = autoria;
        Data = data;
    }

    /// <summary>Proposicao a qual a emenda se vincula.</summary>
    public ProposicaoId ProposicaoId { get; private set; }

    /// <summary>Texto da emenda.</summary>
    public string Texto { get; private set; } = default!;

    /// <summary>Autoria da emenda.</summary>
    public Autoria Autoria { get; private set; }

    /// <summary>Data de apresentacao da emenda.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Registra uma nova emenda.</summary>
    /// <param name="proposicaoId">Proposicao vinculada.</param>
    /// <param name="texto">Texto da emenda.</param>
    /// <param name="autoria">Autoria da emenda.</param>
    /// <param name="data">Data de apresentacao.</param>
    /// <returns>Nova <see cref="Emenda"/>.</returns>
    /// <exception cref="ArgumentException">Se o texto for vazio.</exception>
    public static Emenda Registrar(ProposicaoId proposicaoId, string texto, Autoria autoria, DateOnly data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);
        return new Emenda(EmendaId.New(), proposicaoId, texto.Trim(), autoria, data);
    }
}
