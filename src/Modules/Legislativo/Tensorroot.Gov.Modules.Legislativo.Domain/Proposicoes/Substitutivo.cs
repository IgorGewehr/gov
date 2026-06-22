using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

/// <summary>Identificador forte de um <see cref="Substitutivo"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct SubstitutivoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="SubstitutivoId"/>.</returns>
    public static SubstitutivoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Modificacao integral a uma proposicao em curso. Entidade do agregado
/// <see cref="Proposicao"/>, vinculada por <see cref="ProposicaoId"/>.
/// </summary>
public sealed class Substitutivo : Entity<SubstitutivoId>
{
    private Substitutivo()
    {
    }

    private Substitutivo(SubstitutivoId id, ProposicaoId proposicaoId, string texto, Autoria autoria, DateOnly data)
        : base(id)
    {
        ProposicaoId = proposicaoId;
        Texto = texto;
        Autoria = autoria;
        Data = data;
    }

    /// <summary>Proposicao a qual o substitutivo se vincula.</summary>
    public ProposicaoId ProposicaoId { get; private set; }

    /// <summary>Texto integral do substitutivo.</summary>
    public string Texto { get; private set; } = default!;

    /// <summary>Autoria do substitutivo.</summary>
    public Autoria Autoria { get; private set; }

    /// <summary>Data de apresentacao do substitutivo.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Registra um novo substitutivo.</summary>
    /// <param name="proposicaoId">Proposicao vinculada.</param>
    /// <param name="texto">Texto integral do substitutivo.</param>
    /// <param name="autoria">Autoria do substitutivo.</param>
    /// <param name="data">Data de apresentacao.</param>
    /// <returns>Novo <see cref="Substitutivo"/>.</returns>
    /// <exception cref="ArgumentException">Se o texto for vazio.</exception>
    public static Substitutivo Registrar(ProposicaoId proposicaoId, string texto, Autoria autoria, DateOnly data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);
        return new Substitutivo(SubstitutivoId.New(), proposicaoId, texto.Trim(), autoria, data);
    }
}
