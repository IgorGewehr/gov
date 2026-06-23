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
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Internal;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto.Coleta;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Providers;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Ponto.Coleta;

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

            // Ordem CORRETA dos interceptors (Tenant -> Audit -> Outbox) centralizada em
            // ModuleInterceptorRegistration: a trilha le o TenantId JA carimbado (W0.2).
            options.AddModuleSaveChangesInterceptors(serviceProvider);
        });

        services.AddScoped<IServidorRepository, ServidorRepository>();
        services.AddScoped<ICargoRepository, CargoRepository>();

        // AUTOSSERVICO ("Minha Folha"): repositorio do vinculo usuario<->servidor + a ancora que
        // resolve o servidor do PROPRIO usuario autenticado (ABAC dado-proprio a prova de bala).
        services.AddScoped<IVinculoServidorUsuarioRepository, VinculoServidorUsuarioRepository>();
        services.AddScoped<IResolvedorServidorDoUsuarioAutenticado, ResolvedorServidorDoUsuarioAutenticado>();

        services.AddScoped<IFolhaDePagamentoRepository, FolhaDePagamentoRepository>();
        services.AddScoped<IRubricaFolhaRepository, RubricaFolhaRepository>();
        services.AddScoped<ITabelasLegaisRepository, TabelasLegaisRepository>();
        services.AddScoped<IServidorRegimeConsulta, ServidorRegimeConsulta>();
        services.AddScoped<IRubricaS1010Consulta, RubricaS1010Consulta>();
        services.AddScoped<IParametrosFolhaProvider, ParametrosFolhaProvider>();
        services.AddScoped<ITabelasLegaisProvider, TabelasLegaisProvider>();
        services.AddScoped<IMarcacaoPontoRepository, MarcacaoPontoRepository>();
        services.AddScoped<IJornadaTrabalhoRepository, JornadaTrabalhoRepository>();
        services.AddScoped<IApuracaoPontoRepository, ApuracaoPontoRepository>();
        services.AddScoped<IServidorPontoConsulta, ServidorPontoConsulta>();
        services.AddScoped<IParametrosPontoProvider, ParametrosPontoProvider>();

        // Coletor de ponto (hardware REP -> AFD -> nosso dominio). Parser do AFD (Domain, puro) e o
        // repositorio do parque de REPs. Drivers de coleta atras de ACL (// TODO(prod: SDK proprietario)):
        // o universal de ARQUIVO e o SIMULADO online (gera AFD de exemplo). A fabrica resolve por marca.
        services.AddScoped<IParserAfd, ParserAfd>();
        services.AddScoped<IRepRepository, RepRepository>();
        services.AddSingleton<IColetorRep, ColetorArquivoAfd>();
        services.AddSingleton<IColetorRep, ColetorRepSimulado>();
        services.AddSingleton<IColetorRepFactory, ColetorRepFactory>();

        // eSocial (M5): repositorio de eventos, provider do empregador, servico de geracao e o GATEWAY
        // atras de ACL — impl. SIMULADA por padrao. // TODO(prod: trocar por ESocialGatewaySoap quando
        // houver creds de Producao Restrita; manter Polly + mTLS + URLs por IOptions/Key Vault).
        services.AddScoped<IEventoESocialRepository, EventoESocialRepository>();
        services.AddScoped<IEmpregadorESocialProvider, EmpregadorESocialProvider>();
        services.AddScoped<GeradorEventoApplicationService>();
        services.AddSingleton<IESocialGateway, ESocialGatewaySimulado>();

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
