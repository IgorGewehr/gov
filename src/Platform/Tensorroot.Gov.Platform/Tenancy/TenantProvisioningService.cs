using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Platform.Persistence;

namespace Tensorroot.Gov.Platform.Tenancy;

/// <summary>Provisiona tenants e gerencia as licenças de módulo (onboarding comercial).</summary>
public interface ITenantProvisioningService
{
    /// <summary>Cria um tenant e ativa os módulos informados.</summary>
    /// <param name="cnpj">CNPJ do ente.</param>
    /// <param name="nome">Nome do ente.</param>
    /// <param name="poder">Poder.</param>
    /// <param name="connectionString">Conexão do banco dedicado (opcional; fallback por convenção).</param>
    /// <param name="modulos">Módulos a licenciar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Id do tenant criado.</returns>
    Task<Guid> ProvisionarAsync(string cnpj, string nome, PoderTenant poder, string? connectionString, IEnumerable<string> modulos, CancellationToken cancellationToken);

    /// <summary>Ativa ou desativa a licença de um módulo para um tenant.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="moduleName">Módulo.</param>
    /// <param name="ativo">Estado desejado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task DefinirModuloAsync(Guid tenantId, string moduleName, bool ativo, CancellationToken cancellationToken);

    /// <summary>
    /// Rotaciona a connection string do banco DEDICADO do tenant (troca de segredo/failover/migração)
    /// e INVALIDA o cache em memória — para que a próxima resolução re-busque a conexão nova e nenhuma
    /// instância continue operando sobre o banco antigo (achado K6 da auditoria).
    /// </summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="connectionString">Nova connection string.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task RotacionarConexaoAsync(Guid tenantId, string connectionString, CancellationToken cancellationToken);
}

/// <summary>Implementação EF Core do <see cref="ITenantProvisioningService"/>.</summary>
public sealed class TenantProvisioningService(
    PlatformDbContext context,
    ITenantConnectionCacheInvalidator cacheInvalidator) : ITenantProvisioningService
{
    /// <inheritdoc />
    public async Task<Guid> ProvisionarAsync(string cnpj, string nome, PoderTenant poder, string? connectionString, IEnumerable<string> modulos, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(modulos);

        var tenant = Tenant.Criar(cnpj, nome, poder, connectionString);
        context.Tenants.Add(tenant);
        foreach (var modulo in modulos.Distinct(StringComparer.Ordinal))
        {
            context.TenantModules.Add(new TenantModule(tenant.Id, modulo));
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return tenant.Id;
    }

    /// <inheritdoc />
    public async Task DefinirModuloAsync(Guid tenantId, string moduleName, bool ativo, CancellationToken cancellationToken)
    {
        var vinculo = await context.TenantModules
            .FirstOrDefaultAsync(item => item.TenantId == tenantId && item.ModuleName == moduleName, cancellationToken)
            .ConfigureAwait(false);

        if (vinculo is null)
        {
            vinculo = new TenantModule(tenantId, moduleName);
            context.TenantModules.Add(vinculo);
        }

        if (ativo)
        {
            vinculo.Ativar();
        }
        else
        {
            vinculo.Desativar();
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RotacionarConexaoAsync(Guid tenantId, string connectionString, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var tenant = await context.Tenants
            .FirstOrDefaultAsync(item => item.Id == tenantId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Tenant {tenantId} não encontrado para rotação de conexão.");

        tenant.DefinirConexao(connectionString);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Fecha a janela de divergência: a próxima resolução re-busca a conexão nova no catálogo.
        cacheInvalidator.Invalidar(tenantId);
    }
}
