using System.Text.Json;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Enfileira eventos de integração no Outbox do <see cref="ModuleDbContext"/> ativo no escopo (resolvido
/// via <see cref="ScopeDbContextHolder"/>). A mensagem é adicionada ao change tracker e gravada na mesma
/// transação do comando que a originou — espelha o formato (tipo assembly-qualified + JSON) que o
/// <see cref="OutboxPublisher"/> consome ao drenar o Outbox.
/// </summary>
public sealed class ModuleIntegrationEventWriter(ScopeDbContextHolder holder, TimeProvider timeProvider)
    : IIntegrationEventWriter
{
    /// <inheritdoc />
    public void Enfileirar(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var contexto = holder.Atual
            ?? throw new InvalidOperationException(
                "Nenhum DbContext de módulo foi resolvido neste escopo; não há Outbox onde enfileirar o evento.");

        var tipo = integrationEvent.GetType();
        var tenantId = integrationEvent is IMustHaveTenant comTenant
            ? comTenant.TenantId
            : ExtrairTenantId(integrationEvent);

        var mensagem = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = tipo.AssemblyQualifiedName ?? tipo.FullName!,
            // P0-3: serialize com as opções COMPARTILHADAS (conversor de Value Object), idênticas às do
            // deserialize do OutboxPublisher — integration events com VO de classe round-trip sem poison.
            Content = JsonSerializer.Serialize(integrationEvent, tipo, OutboxSerialization.Options),
            OccurredOnUtc = integrationEvent.OccurredOnUtc == default
                ? timeProvider.GetUtcNow().UtcDateTime
                : integrationEvent.OccurredOnUtc,
        };

        contexto.OutboxMessages.Add(mensagem);
    }

    // Eventos de integração carregam TenantId como propriedade (convenção), mas não implementam
    // IMustHaveTenant (vivem em *.Contracts, sem dependência do SharedKernel.IMustHaveTenant na assinatura).
    private static Guid ExtrairTenantId(IIntegrationEvent integrationEvent)
    {
        var prop = integrationEvent.GetType().GetProperty("TenantId");
        return prop?.GetValue(integrationEvent) is Guid id ? id : Guid.Empty;
    }
}
