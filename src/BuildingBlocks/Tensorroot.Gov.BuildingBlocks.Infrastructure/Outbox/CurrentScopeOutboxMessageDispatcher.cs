using MediatR;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Despachante default: publica no <see cref="IPublisher"/> do escopo ATUAL (sem criar escopo novo).
/// <para>
/// Não isola por escopo — adequado a hosts simples e a testes onde o lote não cruza módulos. O
/// isolamento real (um escopo de DI por mensagem, com tenant carimbado) é provido pelo despachante
/// do <c>ApiHost</c>, que tem acesso a <c>IServiceScopeFactory</c> + <c>TenantOverride</c> (camada
/// superior). Mantê-lo aqui preserva o layering: BuildingBlocks não conhece ApiHost/Platform.
/// </para>
/// </summary>
public sealed class CurrentScopeOutboxMessageDispatcher(IPublisher publisher) : IOutboxMessageDispatcher
{
    /// <inheritdoc />
    public Task DespacharAsync(object evento, Guid tenantId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(evento);
        return publisher.Publish(evento, cancellationToken);
    }
}
