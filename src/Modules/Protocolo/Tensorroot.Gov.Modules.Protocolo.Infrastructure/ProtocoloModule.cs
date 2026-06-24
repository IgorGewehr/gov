using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Application.Processos;
using Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.Modules.Protocolo.Infrastructure.Protocolo;
using Tensorroot.Gov.Modules.Protocolo.Infrastructure.Protocolo.Carimbo;
using Tensorroot.Gov.Modules.Protocolo.Infrastructure.Protocolo.Temporalidade;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure;

/// <summary>
/// Módulo Protocolo: registra o DbContext (provider por configuração), interceptors, repositórios,
/// serviços (gerador de NUP e carimbo de tempo), handlers (MediatR) e validators; expõe os endpoints.
/// Ativável por tenant via descoberta no ApiHost.
/// </summary>
public sealed class ProtocoloModule : IModule
{
    /// <inheritdoc />
    public string Name => "Protocolo";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<ProtocoloDbContext>((serviceProvider, options) =>
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

            // Ordem CORRETA dos interceptors (Tenant -> Audit -> Outbox) centralizada em
            // ModuleInterceptorRegistration: a trilha le o TenantId JA carimbado (W0.2).
            options.AddModuleSaveChangesInterceptors(serviceProvider);
        });

        services.AddScoped<IProcessoRepository, ProcessoRepository>();
        services.AddScoped<IDocumentoRepository, DocumentoRepository>();

        // Temporalidade/destinacao CONARQ (Peca 2 / W9.4): repositorios + motor de calculo local.
        services.AddScoped<IPlanoDeClassificacaoRepository, PlanoDeClassificacaoRepository>();
        services.AddScoped<ITabelaTemporalidadeRepository, TabelaTemporalidadeRepository>();
        services.AddScoped<IDestinacaoProcessoRepository, DestinacaoProcessoRepository>();
        services.AddScoped<IMotorTemporalidade, MotorTemporalidade>();

        // Geração do NUP (sequencial atômico por tenant×ano — Peça 3 / W9.4: contador SequenciaNup
        // sob UPDLOCK/HOLDLOCK em SqlServer, espelhando o hash-chain).
        services.AddScoped<INupGenerator, NupSequencialGenerator>();

        // Carimbo de tempo RFC 3161 (Peça 1 / W9.4): porta ICarimbadorDeTempo atrás de ACL + Polly.
        // Provider "Act" => adapter de produção com HttpClient resiliente (padrão AdnNfseGateway);
        // caso contrário => carimbador LOCAL (fallback/dev). O domínio recusa carimbo local em ato
        // qualificado (criticidade Alta). // TODO(M10): ACT credenciada real + credencial em Key Vault.
        services.Configure<OpcoesAct>(configuration.GetSection(OpcoesAct.Secao));
        var actProvider = configuration["Protocolo:Act:Provider"] ?? "Local";
        if (string.Equals(actProvider, "Act", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<ICarimbadorDeTempo, CarimbadorDeTempoAct>(client =>
                {
                    // Endpoint/credencial da ACT vêm do Key Vault (M2), nunca do repositório.
                    var endpoint = configuration["Protocolo:Act:Endpoint"] ?? "https://act.invalido.local/";
                    client.BaseAddress = new Uri(endpoint);
                })
                .AddStandardResilienceHandler();
        }
        else
        {
            services.AddScoped<ICarimbadorDeTempo, CarimbadorDeTempoLocalService>();
        }

        // Contrato LEGADO do carimbo local (mantido como adapter; não recebe hash de entrada).
        services.AddScoped<ICarimboDeTempoService, CarimbadorDeTempoLocalService>();

        // PORTAL DO CIDADAO (M8): porta de LEITURA cidada (Contracts). "Meus processos" por DOCUMENTO do
        // interessado resolvido server-side pelo modulo Cidadao, respeitando o NivelDeAcesso (so publicos).
        services.AddScoped<Tensorroot.Gov.Modules.Protocolo.Contracts.IConsultaProcessoCidadao, PortalCidadao.ConsultaProcessoCidadao>();

        var applicationAssembly = typeof(AutuarProcessoCommand).Assembly;
        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => ProtocoloEndpoints.Map(endpoints);

    /// <inheritdoc />
    public async Task MigrarBancoAsync(string connectionString, string provider, Guid tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<ProtocoloDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        await using var contexto = new ProtocoloDbContext(construtor.Options, SistemaTenantContext.Instancia);
        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DrenarOutboxAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var contexto = serviceProvider.GetRequiredService<ProtocoloDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }
}
