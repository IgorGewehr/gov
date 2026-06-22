using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Platform.Persistence;

namespace Tensorroot.Gov.Platform.Tenancy;

/// <summary>Consulta quais módulos um tenant tem licenciados (usado pelo gating de runtime).</summary>
public interface ITenantModuleProvider
{
    /// <summary>Indica se um módulo está licenciado e ativo para o tenant.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="moduleName">Nome do módulo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se licenciado e ativo.</returns>
    Task<bool> IsModuleEnabledAsync(Guid tenantId, string moduleName, CancellationToken cancellationToken);

    /// <summary>Lista os módulos licenciados e ativos do tenant.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Nomes dos módulos ativos.</returns>
    Task<IReadOnlyList<string>> EnabledModulesAsync(Guid tenantId, CancellationToken cancellationToken);
}

/// <summary>Implementação EF Core do <see cref="ITenantModuleProvider"/>.</summary>
public sealed class TenantModuleProvider(PlatformDbContext context) : ITenantModuleProvider
{
    /// <inheritdoc />
    public Task<bool> IsModuleEnabledAsync(Guid tenantId, string moduleName, CancellationToken cancellationToken)
        => context.TenantModules.AnyAsync(
            vinculo => vinculo.TenantId == tenantId && vinculo.ModuleName == moduleName && vinculo.Ativo,
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> EnabledModulesAsync(Guid tenantId, CancellationToken cancellationToken)
        => await context.TenantModules
            .Where(vinculo => vinculo.TenantId == tenantId && vinculo.Ativo)
            .Select(vinculo => vinculo.ModuleName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
