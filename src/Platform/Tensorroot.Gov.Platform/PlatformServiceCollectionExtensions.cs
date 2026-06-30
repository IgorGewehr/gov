using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.Platform.Persistence;
using Tensorroot.Gov.Platform.Tenancy;

namespace Tensorroot.Gov.Platform;

/// <summary>Registro de DI da Plataforma (catálogo de tenants e licenciamento de módulos).</summary>
public static class PlatformServiceCollectionExtensions
{
    /// <summary>Registra o <see cref="PlatformDbContext"/>, o provider de módulos e o provisionamento.</summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="provider">Provider de banco ("Sqlite" ou "SqlServer").</param>
    /// <param name="connectionString">String de conexão.</param>
    /// <returns>A própria coleção.</returns>
    public static IServiceCollection AddPlatform(this IServiceCollection services, string provider, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<PlatformDbContext>((sp, options) =>
        {
            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }

            // R3: auditoria imutável (hash-chain) TAMBÉM no control-plane — provisionamento de tenant,
            // licenças de módulo e índice email→tenant deixam de ficar fora da trilha. Reusa o
            // interceptor das módulos; entidades sem tenant são seladas numa cadeia única (Guid.Empty)
            // no banco da plataforma (schema "plataforma").
            options.AddInterceptors(new AuditSaveChangesInterceptor(
                sp.GetRequiredService<ICurrentUser>(),
                sp.GetRequiredService<TimeProvider>()));
        });

        services.AddScoped<ITenantModuleProvider, TenantModuleProvider>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<IUsuarioTenantIndexService, UsuarioTenantIndexService>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<TenantConnectionCache>();
        services.AddSingleton<ITenantConnectionCacheInvalidator>(sp => sp.GetRequiredService<TenantConnectionCache>());
        // SEC-1: protetor de connection string em repouso (envelope AES-256-GCM + KEK via IProvedorKek,
        // registrado pelo módulo Cofre). Singleton, sem estado — depende só do provedor de KEK.
        services.AddSingleton<ProtetorConexaoTenant>();
        services.AddScoped<ITenantConnectionResolver, TenantConnectionResolver>();
        return services;
    }
}
