namespace Tensorroot.Gov.SharedKernel.Primitives;

/// <summary>
/// Contrato não-genérico para acesso aos eventos de domínio acumulados numa entidade.
/// Permite que o interceptor de Outbox varra o ChangeTracker independentemente do tipo do Id.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>Eventos de domínio acumulados e ainda não despachados.</summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>Remove os eventos de domínio acumulados (após materialização no Outbox).</summary>
    void ClearDomainEvents();
}
