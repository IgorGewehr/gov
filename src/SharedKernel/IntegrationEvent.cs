using MediatR;

namespace Tensorroot.Gov.SharedKernel;

/// <summary>
/// Evento de integração: fato publicado de um módulo (Bounded Context) para outros,
/// de forma confiável, através do Outbox Pattern.
/// </summary>
public interface IIntegrationEvent : INotification
{
    /// <summary>Identificador único do evento.</summary>
    Guid EventId { get; }

    /// <summary>Data/hora (UTC) de ocorrência do evento.</summary>
    DateTime OccurredOnUtc { get; }
}

/// <summary>Base imutável (record) para eventos de integração.</summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredOnUtc">Data/hora (UTC) de ocorrência.</param>
public abstract record IntegrationEvent(Guid EventId, DateTime OccurredOnUtc) : IIntegrationEvent;
