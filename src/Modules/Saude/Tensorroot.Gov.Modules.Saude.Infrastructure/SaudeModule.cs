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
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Pacientes;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Integracoes;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Repositories;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure;

/// <summary>
/// Modulo Saude: registra o DbContext (provider por configuracao), interceptors, repositorios,
/// gateways/ACLs (CADSUS, CNES, RNDS, SISAB, SISREG, ICP-Brasil), handlers (MediatR) e validators;
/// expoe os endpoints. Ativavel por tenant via descoberta no ApiHost.
/// </summary>
public sealed class SaudeModule : IModule
{
    /// <inheritdoc />
    public string Name => "Saude";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<SaudeDbContext>((serviceProvider, options) =>
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

        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<IAtendimentoRepository, AtendimentoRepository>();
        services.AddScoped<ISolicitacaoRegulacaoRepository, SolicitacaoRegulacaoRepository>();

        // Gateways/ACLs governamentais: implementacao simulada para dev/testes. Em producao,
        // HTTP resiliente (Polly) atras de Anti-Corruption Layer, com certificados no Azure Key Vault.
        services.AddScoped<ICadsusGateway, SimuladoCadsusGateway>();
        services.AddScoped<IEstabelecimentoRepository, SimuladoEstabelecimentoRepository>();
        services.AddScoped<IRndsGateway, SimuladoRndsGateway>();
        services.AddScoped<ISisabGateway, SimuladoSisabGateway>();
        services.AddScoped<ISisregGateway, SimuladoSisregGateway>();
        services.AddScoped<IAssinaturaIcpBrasilService, SimuladoAssinaturaIcpBrasilService>();
        services.AddScoped<ICotaRepository, SimuladoCotaRepository>();

        var applicationAssembly = typeof(CadastrarPacienteCommand).Assembly;
        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => SaudeEndpoints.Map(endpoints);

    /// <inheritdoc />
    public async Task MigrarBancoAsync(string connectionString, string provider, Guid tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<SaudeDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        await using var contexto = new SaudeDbContext(construtor.Options, SistemaTenantContext.Instancia);
        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DrenarOutboxAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var contexto = serviceProvider.GetRequiredService<SaudeDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }
}
