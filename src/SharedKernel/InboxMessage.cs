namespace Tensorroot.Gov.SharedKernel;

/// <summary>
/// Linha do padrão Inbox (deduplicação de CONSUMO): registra que um evento de integração já foi
/// processado por um handler específico de um tenant, tornando a entrega <b>at-least-once</b> do
/// Outbox em consumo <b>efetivamente exactly-once</b> (idempotência system-wide).
/// <para>
/// A chave de idempotência é a TRINCA (<see cref="EventId"/>, <see cref="Handler"/>,
/// <see cref="TenantId"/>): o MESMO evento pode ser consumido por VÁRIOS handlers (até de módulos
/// distintos) — cada par handler+tenant deduplica de forma independente. Uma redrenagem do Outbox
/// (após falha parcial, reinício, ou backoff) reentrega o evento; o Inbox garante que o efeito
/// colateral do handler já marcado NÃO se repita.
/// </para>
/// <para>
/// Persistido no banco DEDICADO do tenant, no schema do MÓDULO do consumidor — na MESMA transação do
/// efeito do handler (o registro do Inbox e a mutação de estado confirmam juntos via UnitOfWork), de
/// modo que "marcado como consumido" e "efeito aplicado" são atômicos. É <see cref="IMustHaveTenant"/>:
/// o Global Query Filter o isola por tenant como qualquer entidade de negócio.
/// </para>
/// </summary>
public sealed class InboxMessage : IMustHaveTenant
{
    /// <summary>Identificador da linha do Inbox.</summary>
    public Guid Id { get; init; }

    /// <summary>Tenant (ente público) dono do consumo.</summary>
    public Guid TenantId { get; init; }

    /// <summary>
    /// Identificador do evento de integração consumido (<c>IIntegrationEvent.EventId</c>). Junto com
    /// <see cref="Handler"/> e <see cref="TenantId"/> forma a chave única de deduplicação.
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Identidade ESTÁVEL do consumidor (nome completo do tipo do handler). Permite que o mesmo evento
    /// seja deduplicado de forma INDEPENDENTE por cada handler — um handler já tê-lo processado não
    /// impede outro handler (de outro módulo) de processá-lo.
    /// </summary>
    public required string Handler { get; init; }

    /// <summary>Nome do tipo (assembly-qualified) do evento consumido — para diagnóstico/auditoria.</summary>
    public string? EventType { get; init; }

    /// <summary>Data/hora (UTC) em que o consumo foi efetivado e selado no Inbox.</summary>
    public DateTime ProcessedOnUtc { get; init; }
}
