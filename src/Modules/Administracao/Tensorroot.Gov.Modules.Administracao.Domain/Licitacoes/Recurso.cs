using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

/// <summary>Identificador forte de um <see cref="Recurso"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RecursoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RecursoId"/>.</returns>
    public static RecursoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Impugnacao administrativa interposta no certame por um licitante.</summary>
public sealed class Recurso : Entity<RecursoId>
{
    private Recurso()
    {
    }

    private Recurso(RecursoId id, Guid fornecedorId, string fundamentacao, DateOnly dataInterposicao)
        : base(id)
    {
        FornecedorId = fornecedorId;
        Fundamentacao = fundamentacao;
        DataInterposicao = dataInterposicao;
    }

    /// <summary>Licitante recorrente.</summary>
    public Guid FornecedorId { get; private set; }

    /// <summary>Fundamentacao do recurso.</summary>
    public string Fundamentacao { get; private set; } = default!;

    /// <summary>Data de interposicao.</summary>
    public DateOnly DataInterposicao { get; private set; }

    /// <summary>Indica se o recurso foi provido (nulo enquanto pendente).</summary>
    public bool? Provido { get; private set; }

    /// <summary>Interpoe um novo recurso.</summary>
    /// <param name="fornecedorId">Licitante recorrente.</param>
    /// <param name="fundamentacao">Fundamentacao do recurso.</param>
    /// <param name="dataInterposicao">Data de interposicao.</param>
    /// <returns>Novo <see cref="Recurso"/>.</returns>
    /// <exception cref="ArgumentException">Se a fundamentacao for vazia.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o fornecedor nao for informado.</exception>
    public static Recurso Interpor(Guid fornecedorId, string fundamentacao, DateOnly dataInterposicao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentacao);
        if (fornecedorId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(fornecedorId), "Fornecedor e obrigatorio no recurso.");
        }

        return new Recurso(RecursoId.New(), fornecedorId, fundamentacao, dataInterposicao);
    }

    /// <summary>Julga o recurso, definindo seu provimento.</summary>
    /// <param name="provido"><c>true</c> se provido; caso contrario, <c>false</c>.</param>
    public void Julgar(bool provido) => Provido = provido;
}
