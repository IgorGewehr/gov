using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Providers;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure;

/// <summary>
/// Modulo RecursosHumanos: registra o DbContext (provider por configuracao), interceptors, repositorios,
/// providers de configuracao/consulta, handlers (MediatR) e validators; expoe os endpoints. Ativavel por
/// tenant via descoberta no ApiHost.
/// </summary>
public sealed class RecursosHumanosModule : IModule
{
    /// <inheritdoc />
    public string Name => "RecursosHumanos";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<RecursosHumanosDbContext>((serviceProvider, options) =>
        {
            // Banco DEDICADO por tenant: a conexao vem do catalogo da plataforma.
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

        services.AddScoped<IServidorRepository, ServidorRepository>();
        services.AddScoped<ICargoRepository, CargoRepository>();
        services.AddScoped<IFolhaDePagamentoRepository, FolhaDePagamentoRepository>();
        services.AddScoped<IRubricaFolhaRepository, RubricaFolhaRepository>();
        services.AddScoped<ITabelasLegaisRepository, TabelasLegaisRepository>();
        services.AddScoped<IServidorRegimeConsulta, ServidorRegimeConsulta>();
        services.AddScoped<IRubricaS1010Consulta, RubricaS1010Consulta>();
        services.AddScoped<IParametrosFolhaProvider, ParametrosFolhaProvider>();
        services.AddScoped<ITabelasLegaisProvider, TabelasLegaisProvider>();

        var applicationAssembly = typeof(AdmitirServidorCommand).Assembly;
        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => RecursosHumanosEndpoints.Map(endpoints);

    /// <inheritdoc />
    public async Task MigrarBancoAsync(string connectionString, string provider, Guid tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<RecursosHumanosDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        await using var contexto = new RecursosHumanosDbContext(construtor.Options, SistemaTenantContext.Instancia);
        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DrenarOutboxAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var contexto = serviceProvider.GetRequiredService<RecursosHumanosDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }
}
