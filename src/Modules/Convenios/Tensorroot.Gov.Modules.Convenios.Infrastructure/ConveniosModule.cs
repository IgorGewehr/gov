using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;
using Tensorroot.Gov.Modules.Convenios.Application;
using Tensorroot.Gov.Modules.Convenios.Application.Abstractions;
using Tensorroot.Gov.Modules.Convenios.Domain.Parametros;
using Tensorroot.Gov.Modules.Convenios.Infrastructure.Integracoes;
using Tensorroot.Gov.Modules.Convenios.Infrastructure.Parametros;
using Tensorroot.Gov.Modules.Convenios.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Convenios.Infrastructure.Persistence.Repositories;

namespace Tensorroot.Gov.Modules.Convenios.Infrastructure;

/// <summary>
/// Modulo Convenios (W9.6): hospeda os DOIS fluxos separados — convenios federais RECEBIDOS (Dec.
/// 11.531/2023) e parcerias-saida OSC/MROSC (Lei 13.019/2014). Registra o <see cref="ConveniosDbContext"/>
/// (schema "convenios" + Outbox/interceptors), repositorios, a porta de parametros por tenant
/// (<see cref="IConveniosParametros"/>), o ACL do Transferegov (simulado no M9), handlers (MediatR) e
/// validators; expoe os endpoints. Consome <c>ICalendarioDiasUteis</c> e <c>TimeProvider</c> do
/// Composition Root (W9.1) — nao os recria. Ativavel por tenant via descoberta no ApiHost.
/// </summary>
public sealed class ConveniosModule : IModule
{
    /// <inheritdoc />
    public string Name => "Convenios";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<ConveniosDbContext>((serviceProvider, options) =>
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

            // Ordem CORRETA dos interceptors (Tenant -> Audit -> Outbox) centralizada (a trilha le o TenantId
            // JA carimbado). UoW + IntegrationEventWriter sao resolvidos pelo ScopeDbContextHolder central.
            options.AddModuleSaveChangesInterceptors(serviceProvider);
        });

        // Repositorios dos dois agregados-raiz.
        services.AddScoped<IConvenioRecebidoRepository, ConvenioRecebidoRepository>();
        services.AddScoped<IParceriaOscRepository, ParceriaOscRepository>();

        // Parametros por tenant (prazos/percentuais/Selic com norma-fonte — sem numero magico, S16).
        services.AddScoped<IConveniosParametros, ConveniosParametrosConfiguracao>();

        // ACL do Transferegov.br (fluxo A). M9: SIMULADO (leitura DTPAR deterministica/WireMock).
        // TODO(M10): trocar por gateway HTTP real atras de Polly (AddStandardResilienceHandler) + creds/cert.
        services.AddScoped<ITransferegovGateway, SimuladoTransferegovGateway>();

        var applicationAssembly = typeof(Tensorroot.Gov.Modules.Convenios.Application.AssemblyReference).Assembly;
        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => ConveniosEndpoints.Map(endpoints);

    /// <inheritdoc />
    public async Task MigrarBancoAsync(string connectionString, string provider, Guid tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<ConveniosDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        await using var contexto = new ConveniosDbContext(construtor.Options, SistemaTenantContext.Instancia);
        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DrenarOutboxAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var contexto = serviceProvider.GetRequiredService<ConveniosDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }
}
