namespace Tensorroot.Gov.SharedKernel.Primitives;

/// <summary>
/// Raiz de agregado: ponto de entrada e fronteira de consistência transacional
/// de um conjunto de entidades e objetos de valor relacionados.
/// </summary>
/// <typeparam name="TId">Tipo do identificador da raiz de agregado.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    /// <summary>Inicializa a raiz de agregado com o identificador informado.</summary>
    /// <param name="id">Identificador único.</param>
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>Construtor sem parâmetros exigido pelo materializador do EF Core.</summary>
    protected AggregateRoot()
    {
    }
}
