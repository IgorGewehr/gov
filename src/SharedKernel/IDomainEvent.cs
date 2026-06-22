using MediatR;

namespace Tensorroot.Gov.SharedKernel;

/// <summary>
/// Evento de domínio: fato relevante ocorrido dentro de um agregado, despachado
/// in-process (via MediatR) após a persistência da transação que o originou.
/// </summary>
public interface IDomainEvent : INotification
{
}
