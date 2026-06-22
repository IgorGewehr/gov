using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        if (string.Equals(provedorKek, "KeyVault", StringComparison.OrdinalIgnoreCase))
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
