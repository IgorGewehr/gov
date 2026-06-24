using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;
using Tensorroot.Gov.Platform.Persistence;
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

    /// <summary>
    /// FAN-OUT de migração dos tenants JÁ EXISTENTES (database-per-tenant — W9.7). Re-executa, de forma
    /// IDEMPOTENTE, a migração de schema de cada módulo licenciado no banco DEDICADO de cada tenant ativo.
    /// É como uma evolução de schema (ex.: a tabela InboxMessages) alcança os bancos provisionados ANTES
    /// do deploy: <c>MigrateAsync</c> só aplica migrations pendentes, e o <c>SchemaProvisioner</c> é
    /// idempotente — re-rodar é seguro e não toca dados. Chamado no startup (após migrar o banco de
    /// controle), uma vez, percorrendo o catálogo da plataforma.
    /// <para>
    /// Resiliente por tenant/módulo: a falha de um não interrompe os demais (a fundação não pode travar o
    /// boot inteiro por um banco indisponível). // TODO(M10): rotação de connection string vinda do Key
    /// Vault em produção + invalidação de cache (<c>ITenantConnectionCacheInvalidator</c>) já existem; o
    /// agendamento operacional (janela de manutenção / migração online) fica para a infra de produção.
    /// </para>
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    public async Task MigrarTenantsExistentesAsync(CancellationToken cancellationToken)
    {
        var provider = configuration["Database:Provider"] ?? "Sqlite";

        List<(Guid Id, string? ConnectionString)> tenants;
        Dictionary<Guid, HashSet<string>> licencas;
        await using (var escopo = scopeFactory.CreateAsyncScope())
        {
            var plataforma = escopo.ServiceProvider.GetRequiredService<PlatformDbContext>();
            tenants = (await plataforma.Tenants
                    .Where(tenant => tenant.Ativo)
                    .Select(tenant => new { tenant.Id, tenant.ConnectionString })
                    .ToListAsync(cancellationToken).ConfigureAwait(false))
                .Select(tenant => (tenant.Id, tenant.ConnectionString))
                .ToList();

            var pares = await plataforma.TenantModules
                .Where(licenca => licenca.Ativo)
                .Select(licenca => new { licenca.TenantId, licenca.ModuleName })
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            licencas = pares
                .GroupBy(par => par.TenantId)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.Select(par => par.ModuleName).ToHashSet(StringComparer.Ordinal));
        }

        foreach (var (tenantId, connectionString) in tenants)
        {
            if (!licencas.TryGetValue(tenantId, out var licenciados))
            {
                continue;
            }

            var conexaoBruta = string.IsNullOrWhiteSpace(connectionString)
                ? TenantConnectionResolver.ConexaoPadrao(tenantId)
                : connectionString;

            // A conexão no catálogo pode estar PROTEGIDA (envelope AES-256-GCM + KEK, SEC-1) em produção.
            // Decifra-a só em memória para migrar; em dev/SQLite (em claro) é no-op.
            string conexao;
            await using (var escopoConexao = scopeFactory.CreateAsyncScope())
            {
                var protetor = escopoConexao.ServiceProvider.GetRequiredService<ProtetorConexaoTenant>();
                conexao = ProtetorConexaoTenant.EstaProtegida(conexaoBruta)
                    ? await protetor.RevelarAsync(tenantId, conexaoBruta, cancellationToken).ConfigureAwait(false)
                    : conexaoBruta;
            }

            foreach (var module in modules.Where(modulo => licenciados.Contains(modulo.Name)))
            {
                await using var escopoModulo = scopeFactory.CreateAsyncScope();
                await module
                    .MigrarBancoAsync(conexao, provider, tenantId, escopoModulo.ServiceProvider, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
