using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Assinatura;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;
using Tensorroot.Gov.Modules.Cofre.Application.Abstractions;
using Tensorroot.Gov.Modules.Cofre.Infrastructure.Cripto;
using Tensorroot.Gov.Modules.Cofre.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure;

/// <summary>
/// Modulo Cofre (custodia + assinatura A1, transversal). Registra o <see cref="CofreDbContext"/> no
/// banco DEDICADO do tenant, o provedor de KEK (KeyVault em PROD; Config em DEV — A1-DESIGN §1), o
/// nucleo criptografico, o repositorio, a custodia (cadastro/rotacao) e a porta transversal
/// <see cref="IServicoAssinaturaDigital"/>. E SEGURANCA CRITICA.
/// </summary>
public sealed class CofreModule : IModule
{
    /// <inheritdoc />
    public string Name => "Cofre";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<CofreDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<ITenantConnectionResolver>().ResolveConnectionString();
            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }

            // Ordem CORRETA dos interceptors (Tenant -> Audit -> Outbox) centralizada em
            // ModuleInterceptorRegistration: a trilha le o TenantId JA carimbado (W0.2).
            options.AddModuleSaveChangesInterceptors(serviceProvider);
        });

        // Opcoes do cofre + provedor de KEK (envelope encryption — A1-DESIGN §1).
        services.Configure<CofreOptions>(configuration.GetSection(CofreOptions.Secao));
        var provedorKek = configuration[$"{CofreOptions.Secao}:ProvedorKek"] ?? "Config";

        // FAIL-FAST DE PRODUCAO (CF-2, CLAUDE.md §6 "segredos so no Key Vault"): em PROD a KEK
        // (chave-mestra que embrulha TODAS as DEKs de TODOS os tenants) so pode vir do Key Vault/HSM.
        // Sem esta trava o sistema subia silenciosamente com a KEK de DEV (provedor "Config", KEK AES
        // de config/env) — dump de config/env expoe a KEK e vaza o A1 de todos os tenants. Espelha o
        // fail-fast de Database:Provider (Program.cs): em ambiente nao-Development, exigir KeyVault.
        //
        // O ambiente do Host chega na configuracao SOB A CHAVE CANONICA HostDefaults.EnvironmentKey
        // ("environment") — populada tanto por UseEnvironment(...) (programatico) quanto pelas variaveis
        // ASPNETCORE_/DOTNET_ENVIRONMENT (a CreateBuilder normaliza ambas nessa chave). Lemos "environment"
        // PRIMEIRO (assim respeitamos UseEnvironment, como o app.Environment.IsDevelopment() do Program.cs)
        // e so depois caimos nas variaveis cruas. Ausencia total => PRODUCAO (deny-by-default).
        var ambiente = configuration[HostDefaults.EnvironmentKey]
            ?? configuration["ASPNETCORE_ENVIRONMENT"]
            ?? configuration["DOTNET_ENVIRONMENT"]
            ?? Environments.Production;
        var ehDesenvolvimento = string.Equals(ambiente, Environments.Development, StringComparison.OrdinalIgnoreCase);
        var ehKeyVault = string.Equals(provedorKek, "KeyVault", StringComparison.OrdinalIgnoreCase);
        if (!ehDesenvolvimento && !ehKeyVault)
        {
            throw new InvalidOperationException(
                $"Cofre:ProvedorKek invalido em ambiente '{ambiente}': '{provedorKek}'. " +
                "PRODUCAO exige Cofre:ProvedorKek=KeyVault (KEK no Key Vault/HSM; a chave-mestra nunca " +
                "sai do HSM). O provedor 'Config' (KEK AES de config/env) e EXCLUSIVO de DESENVOLVIMENTO " +
                "— subir com ele em producao colocaria a chave-mestra de todos os tenants fora do HSM.");
        }

        if (ehKeyVault)
        {
            // PRODUCAO: KEK no Key Vault/HSM (wrap/unwrap; a chave nunca sai do HSM).
            services.AddSingleton<IProvedorKek, ProvedorKekKeyVault>();
        }
        else
        {
            // DEV: KEK de config FORA do repo. // TODO(prod: Key Vault wrap/unwrap).
            services.AddSingleton<IProvedorKek, ProvedorKekConfig>();
        }

        services.AddScoped<CofreCripto>();
        services.AddScoped<ICofreCertificadoRepository, CofreCertificadoRepository>();
        services.AddScoped<ServicoCustodiaCertificado>();
        services.AddScoped<IServicoAssinaturaDigital, ServicoAssinaturaDigital>();
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => CofreEndpoints.Map(endpoints);

    /// <inheritdoc />
    public async Task MigrarBancoAsync(string connectionString, string provider, Guid tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<CofreDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        await using var contexto = new CofreDbContext(construtor.Options, SistemaTenantContext.Instancia);
        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DrenarOutboxAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var contexto = serviceProvider.GetRequiredService<CofreDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }
}
