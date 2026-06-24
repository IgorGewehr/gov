namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Inbox;

/// <summary>
/// Guarda de idempotência de CONSUMO (padrão Inbox), system-wide. Decide se um evento de integração já
/// foi processado por um handler/tenant (deduplicação) e SELA o consumo na MESMA transação do efeito do
/// handler — transformando a entrega at-least-once do Outbox em consumo efetivamente exactly-once.
/// <para>
/// Vive em BuildingBlocks (fundação) e opera sobre o <see cref="ModuleDbContext"/> ativo do escopo (o do
/// módulo do consumidor), de modo que QUALQUER consumidor de Integration Event — não só o Convênios —
/// herde a idempotência sem código próprio. O despachante de Outbox chama
/// <see cref="JaProcessadoAsync"/> ANTES e <see cref="RegistrarProcessado"/> DEPOIS do handler.
/// </para>
/// </summary>
public interface IInboxGuard
{
    /// <summary>
    /// Indica se a trinca (<paramref name="eventId"/>, <paramref name="handler"/>, tenant atual) JÁ foi
    /// processada e selada no Inbox do módulo — caso em que o handler deve ser PULADO (entrega duplicada).
    /// </summary>
    /// <param name="eventId">Identificador do evento de integração (<c>IIntegrationEvent.EventId</c>).</param>
    /// <param name="handler">Identidade estável do consumidor (nome completo do tipo do handler).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se já consumido (pular); <c>false</c> se é a primeira vez.</returns>
    Task<bool> JaProcessadoAsync(Guid eventId, string handler, CancellationToken cancellationToken);

    /// <summary>
    /// ENFILEIRA (Add no <see cref="ModuleDbContext"/> do escopo, sem SaveChanges) a marca de consumo da
    /// trinca evento+handler+tenant. O selo confirma JUNTO com o efeito do handler (UnitOfWork), tornando
    /// "marcado como consumido" e "efeito aplicado" atômicos. Se o handler não persistir nada, o despachante
    /// confirma o Inbox explicitamente (ver <c>ScopedOutboxMessageDispatcher</c>).
    /// </summary>
    /// <param name="eventId">Identificador do evento de integração.</param>
    /// <param name="handler">Identidade estável do consumidor.</param>
    /// <param name="eventType">Nome do tipo do evento (diagnóstico).</param>
    void RegistrarProcessado(Guid eventId, string handler, string? eventType);
}
