using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Enfileira um <see cref="IIntegrationEvent"/> no Outbox do módulo ativo no escopo, de forma
/// transacional: a mensagem é gravada na MESMA unidade de trabalho que o estado que a originou
/// (consistência do Outbox Pattern). A publicação efetiva (via MediatR) é feita depois pelo
/// <c>IOutboxPublisher</c> ao drenar o Outbox. Não chama <c>SaveChanges</c> — quem dispara o
/// comando confirma a transação.
/// </summary>
public interface IIntegrationEventWriter
{
    /// <summary>Adiciona o evento de integração ao Outbox (pendente), sem confirmar a transação.</summary>
    /// <param name="integrationEvent">Evento de integração a publicar.</param>
    void Enfileirar(IIntegrationEvent integrationEvent);
}
