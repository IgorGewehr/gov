using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure;

/// <summary>
/// Ponto ÚNICO de registro dos interceptors de SaveChanges de um <see cref="DbContext"/> de módulo.
/// <para>
/// A ordem importa: o EF Core invoca <c>SavingChanges</c> dos interceptors NA ORDEM DE REGISTRO.
/// Portanto a sequência correta é <b>Tenant → Audit → Outbox</b>:
/// <list type="number">
/// <item><see cref="TenantSaveChangesInterceptor"/> carimba o <c>TenantId</c> nas entidades novas;</item>
/// <item><see cref="AuditSaveChangesInterceptor"/> lê o <c>TenantId</c> JÁ carimbado para a trilha
/// (sem isto a trilha registraria <c>Guid.Empty</c> para entidades cujo factory não define o tenant)
/// e encadeia o hash por tenant;</item>
/// <item><see cref="ConvertDomainEventsToOutboxInterceptor"/> materializa os eventos de domínio na
/// Outbox por último, já com tenant e auditoria consolidados.</item>
/// </list>
/// Antes, cada um dos 13 módulos replicava o <c>AddInterceptors(Audit, Tenant, Outbox)</c> em ordem
/// DIVERGENTE (Audit antes de Tenant), reintroduzindo o risco latente W0.2. Centralizar aqui elimina
/// a divergência: todo módulo chama este helper e herda a ordem correta por construção.
/// </para>
/// </summary>
public static class ModuleInterceptorRegistration
{
    /// <summary>
    /// Adiciona, na ordem CORRETA (Tenant → Audit → Outbox), os interceptors de SaveChanges
    /// resolvidos do container ao <see cref="DbContextOptionsBuilder"/> do módulo.
    /// </summary>
    /// <param name="options">Construtor de opções do DbContext do módulo.</param>
    /// <param name="serviceProvider">Provedor de serviços do escopo do DbContext.</param>
    /// <returns>O próprio <paramref name="options"/>, para encadeamento fluente.</returns>
    public static DbContextOptionsBuilder AddModuleSaveChangesInterceptors(
        this DbContextOptionsBuilder options,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return options.AddInterceptors(
            serviceProvider.GetRequiredService<TenantSaveChangesInterceptor>(),
            serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>(),
            serviceProvider.GetRequiredService<ConvertDomainEventsToOutboxInterceptor>());
    }
}
