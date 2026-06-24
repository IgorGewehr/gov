using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Inbox;

/// <summary>
/// Implementação default do <see cref="IInboxGuard"/>: opera sobre o <see cref="ModuleDbContext"/> ativo
/// do escopo (resolvido pelo <see cref="ScopeDbContextHolder"/> quando o handler toca seu próprio
/// contexto). A deduplicação tem DUAS camadas, ambas necessárias:
/// <list type="number">
/// <item>PRÉ-CHECK barato: consulta o Inbox do módulo; se a trinca já existe, pula o handler (caminho
/// quente da redrenagem — evita reabrir o efeito colateral).</item>
/// <item>UNIQUE no banco (EventId, Handler, TenantId): a garantia DURA. Mesmo sob corrida (duas
/// drenagens simultâneas que ambas passam o pré-check), só UMA inserção sobrevive ao commit; a outra
/// viola a unicidade e é tratada como duplicada — sem aplicar o efeito duas vezes.</item>
/// </list>
/// <para>
/// O contexto é resolvido de forma TARDIA via o holder do escopo: no caminho do despachante de Outbox,
/// o handler resolve o <see cref="ModuleDbContext"/> do seu módulo ao tocar repositórios; este guard usa
/// esse MESMO contexto, de modo que o selo do Inbox confirma na MESMA transação do efeito (UnitOfWork).
/// Quando ainda não há contexto resolvido no escopo (pré-check antes do handler), o pré-check é um no-op
/// conservador (retorna "não processado") e a unicidade no commit faz a guarda real.
/// </para>
/// </summary>
public sealed class InboxGuard(
    ScopeDbContextHolder holder,
    ITenantContext tenantContext,
    TimeProvider timeProvider) : IInboxGuard
{
    /// <inheritdoc />
    public async Task<bool> JaProcessadoAsync(Guid eventId, string handler, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(handler);

        var contexto = holder.Atual;
        if (contexto is null || !tenantContext.HasTenant)
        {
            // Ainda não há contexto de módulo resolvido (ou tenant) neste escopo: a guarda dura é o
            // UNIQUE no commit. Pré-check conservador → segue para o handler.
            return false;
        }

        var tenantId = tenantContext.TenantId;
        return await contexto.Set<InboxMessage>()
            .AnyAsync(
                inbox => inbox.EventId == eventId && inbox.Handler == handler && inbox.TenantId == tenantId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void RegistrarProcessado(Guid eventId, string handler, string? eventType)
    {
        ArgumentException.ThrowIfNullOrEmpty(handler);

        var contexto = holder.Atual
            ?? throw new InvalidOperationException(
                "Inbox: nenhum ModuleDbContext ativo no escopo para selar o consumo. O handler do " +
                "Integration Event deve tocar o contexto do seu módulo (repositório) antes do selo.");

        var tenantId = tenantContext.HasTenant
            ? tenantContext.TenantId
            : throw new InvalidOperationException("Inbox: sem tenant resolvido para selar o consumo.");

        contexto.Set<InboxMessage>().Add(new InboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventId = eventId,
            Handler = handler,
            EventType = eventType,
            ProcessedOnUtc = timeProvider.GetUtcNow().UtcDateTime,
        });
    }
}
