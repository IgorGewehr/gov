using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;
using Tensorroot.Gov.Platform.Tenancy;

namespace Tensorroot.Gov.ApiHost.Provisioning;

/// <summary>
/// Orquestra o provisionamento de um tenant: grava o catálogo da plataforma e MIGRA o
/// banco DEDICADO de cada módulo licenciado (database-per-tenant).
/// </summary>
internal sealed class TenantProvisioner(
    IServiceScopeFactory scopeFactory,
    IEnumerable<IModule> modules,
    IConfiguration configuration)
{
    /// <summary>Provisiona o tenant (catálogo + migração dos bancos dos módulos licenciados).</summary>
    /// <returns>Id do tenant provisionado.</returns>
    public async Task<Guid> ProvisionarAsync(
        string cnpj,
        string nome,
        PoderTenant poder,
        string? connectionString,
        IReadOnlyList<string> modulos,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(modulos);

        Guid tenantId;
        await using (var escopo = scopeFactory.CreateAsyncScope())
        {
            var provisioning = escopo.ServiceProvider.GetRequiredService<ITenantProvisioningService>();
            tenantId = await provisioning
                .ProvisionarAsync(cnpj, nome, poder, connectionString, modulos, cancellationToken)
                .ConfigureAwait(false);
        }

        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var conexao = string.IsNullOrWhiteSpace(connectionString)
            ? TenantConnectionResolver.ConexaoPadrao(tenantId)
            : connectionString;

        foreach (var module in modules.Where(modulo => modulos.Contains(modulo.Name, StringComparer.Ordinal)))
        {
            // Escopo dedicado por módulo: a semeadura (ex.: índice central de login da Identidade)
            // resolve serviços com escopo (PlatformDbContext) sem vazar entre módulos.
            await using var escopoModulo = scopeFactory.CreateAsyncScope();
            await module
                .MigrarBancoAsync(conexao, provider, tenantId, escopoModulo.ServiceProvider, cancellationToken)
                .ConfigureAwait(false);
        }

        return tenantId;
    }
}
