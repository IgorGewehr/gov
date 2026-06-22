namespace Tensorroot.Gov.SharedKernel.Primitives;

/// <summary>
/// Entidade base do domínio: possui identidade própria e acumula eventos de domínio
/// a serem despachados após a persistência (via interceptor de Outbox).
/// </summary>
/// <typeparam name="TId">Tipo do identificador da entidade.</typeparam>
public abstract class Entity<TId> : IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Inicializa a entidade com o identificador informado.</summary>
    /// <param name="id">Identificador único.</param>
    protected Entity(TId id) => Id = id;

    /// <summary>Construtor sem parâmetros exigido pelo materializador do EF Core.</summary>
    protected Entity()
    {
    }

    /// <summary>Identificador único da entidade.</summary>
    public TId Id { get; protected init; } = default!;

    /// <summary>Eventos de domínio acumulados e ainda não despachados.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    /// <summary>Acrescenta um evento de domínio à entidade.</summary>
    /// <param name="domainEvent">Evento de domínio a registrar.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>Remove todos os eventos de domínio acumulados (após o despacho).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
