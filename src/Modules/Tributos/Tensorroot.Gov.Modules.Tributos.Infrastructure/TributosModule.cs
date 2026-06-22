using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Application.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Application.Nfse;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Nfse;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Routing;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure;

/// <summary>
/// Módulo Tributos: registra o DbContext (provider por configuração), interceptors, repositórios,
/// handlers (MediatR) e validators; expõe os endpoints. Ativável por tenant via descoberta no ApiHost.
/// </summary>
public sealed class TributosModule : IModule
{
    /// <inheritdoc />
    public string Name => "Tributos";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<TributosDbContext>((serviceProvider, options) =>
        {
            // Banco DEDICADO por tenant: a conexão vem do catálogo da plataforma.
            var connectionString = serviceProvider.GetRequiredService<ITenantConnectionResolver>().ResolveConnectionString();
            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }

            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>(),
                serviceProvider.GetRequiredService<TenantSaveChangesInterceptor>(),
                serviceProvider.GetRequiredService<ConvertDomainEventsToOutboxInterceptor>());
        });

        services.AddScoped<IContribuinteRepository, ContribuinteRepository>();
        services.AddScoped<ILancamentoRepository, LancamentoRepository>();
        services.AddScoped<IDividaAtivaRepository, DividaAtivaRepository>();
        services.AddScoped<INotaFiscalServicoRepository, NotaFiscalServicoRepository>();
        services.AddScoped<IImovelRepository, ImovelRepository>();
        services.AddScoped<IPlantaValoresRepository, PlantaValoresRepository>();
        services.AddScoped<ITabelaAliquotaIptuRepository, TabelaAliquotaIptuRepository>();
        services.AddScoped<IDamRepository, DamRepository>();
        services.AddScoped<INfseSincronizador, NfseSincronizador>();

        // Gateway NFS-e/ADN: HTTP resiliente (Polly) em produção; simulado para dev/testes.
        var nfseProvider = configuration["Nfse:Provider"] ?? "Simulado";
        if (string.Equals(nfseProvider, "Adn", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<INfseNacionalGateway, AdnNfseGateway>(client =>
                    client.BaseAddress = new Uri(configuration["Nfse:Adn:BaseUrl"] ?? "https://adn.invalido.local/"))
                .AddStandardResilienceHandler();
        }
        else
        {
            services.AddSingleton<INfseNacionalGateway, SimuladoNfseGateway>();
        }

        var applicationAssembly = typeof(CadastrarContribuintePessoaFisicaCommand).Assembly;
        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        TributosEndpoints.Map(endpoints);
        IptuEndpoints.Map(endpoints);
    }

    /// <inheritdoc />
    public async Task MigrarBancoAsync(string connectionString, string provider, Guid tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<TributosDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        await using var contexto = new TributosDbContext(construtor.Options, SistemaTenantContext.Instancia);
        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DrenarOutboxAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var contexto = serviceProvider.GetRequiredService<TributosDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }
}
