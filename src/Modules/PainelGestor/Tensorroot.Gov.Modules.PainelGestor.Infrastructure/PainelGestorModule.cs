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
using Tensorroot.Gov.Modules.PainelGestor.Application.Abstractions;
using Tensorroot.Gov.Modules.PainelGestor.Application.Ingestao;
using Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Limites;
using Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence.Repositories;

namespace Tensorroot.Gov.Modules.PainelGestor.Infrastructure;

/// <summary>
/// Módulo PainelGestor (M8 — Painel do Gestor + BI): módulo READ-ONLY que materializa read models a
/// partir dos Integration Events publicados pelos módulos-fonte (Finanças, Tributos, RH, Transparencia)
/// — consumidos via Outbox, NUNCA lendo o interno de outro módulo (CLAUDE.md §2). Registra o DbContext
/// (schema isolado "painelgestor"), repositórios, idempotência da ingestão (ACL), provedor de limites
/// LRF parametrizáveis, handlers (MediatR) e validators; expõe os endpoints GET gated por <c>painel.ver</c>.
/// Ativável por tenant via descoberta no ApiHost.
/// </summary>
public sealed class PainelGestorModule : IModule
{
    /// <inheritdoc />
    public string Name => "PainelGestor";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<PainelGestorDbContext>((serviceProvider, options) =>
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

            // Ordem CORRETA dos interceptors (Tenant -> Audit -> Outbox), centralizada em
            // ModuleInterceptorRegistration: a trilha le o TenantId JA carimbado.
            options.AddModuleSaveChangesInterceptors(serviceProvider);
        });

        // Read model consolidado + idempotencia da ingestao (ACL) + provedor de limites LRF (parametrizavel).
        services.AddScoped<IIndicadorMunicipioRepository, IndicadorMunicipioRepository>();
        services.AddScoped<IIngestaoIdempotencia, IngestaoIdempotencia>();
        services.AddScoped<ILimitesPessoalProvider, LimitesPessoalProvider>();

        // Colaborador da ingestao (get-or-create do snapshot do exercicio).
        services.AddScoped<MaterializadorIndicadores>();

        // Handlers de ingestao (consumidores de Integration Events) + query do painel + validators.
        var applicationAssembly = typeof(Application.AssemblyReference).Assembly;
        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => PainelGestorEndpoints.Map(endpoints);

    /// <inheritdoc />
    public async Task MigrarBancoAsync(string connectionString, string provider, Guid tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<PainelGestorDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        await using var contexto = new PainelGestorDbContext(construtor.Options, SistemaTenantContext.Instancia);
        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DrenarOutboxAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        // Modulo read-only: nao publica eventos proprios, mas drena por consistencia (tabela Outbox herdada).
        var contexto = serviceProvider.GetRequiredService<PainelGestorDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }
}
