using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Outbox;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Application.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Application.Dividas;
using Tensorroot.Gov.Modules.Tributos.Application.Nfse;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Dividas;
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

            // Ordem CORRETA dos interceptors (Tenant -> Audit -> Outbox) centralizada em
            // ModuleInterceptorRegistration: a trilha le o TenantId JA carimbado (W0.2).
            options.AddModuleSaveChangesInterceptors(serviceProvider);
        });

        services.AddScoped<IContribuinteRepository, ContribuinteRepository>();
        services.AddScoped<ILancamentoRepository, LancamentoRepository>();
        services.AddScoped<IDividaAtivaRepository, DividaAtivaRepository>();
        services.AddScoped<INotaFiscalServicoRepository, NotaFiscalServicoRepository>();
        services.AddScoped<IImovelRepository, ImovelRepository>();
        services.AddScoped<IPlantaValoresRepository, PlantaValoresRepository>();
        services.AddScoped<ITabelaAliquotaIptuRepository, TabelaAliquotaIptuRepository>();
        services.AddScoped<IDamRepository, DamRepository>();
        services.AddScoped<ITabelaAliquotaIssRepository, TabelaAliquotaIssRepository>();
        services.AddScoped<IApuracaoIssRepository, ApuracaoIssRepository>();
        services.AddScoped<INotaFiscalServicoConsulta, NotaFiscalServicoConsulta>();
        services.AddScoped<IAliquotaItbiRepository, AliquotaItbiRepository>();
        services.AddScoped<ITransmissaoImobiliariaRepository, TransmissaoImobiliariaRepository>();
        services.AddScoped<IProcessoArbitramentoItbiRepository, ProcessoArbitramentoItbiRepository>();
        services.AddScoped<ITabelaTaxaRepository, TabelaTaxaRepository>();
        services.AddScoped<IAlvaraRepository, AlvaraRepository>();
        services.AddScoped<ITabelaCosipRepository, TabelaCosipRepository>();
        services.AddScoped<IObraContribuicaoMelhoriaRepository, ObraContribuicaoMelhoriaRepository>();
        services.AddScoped<INfseSincronizador, NfseSincronizador>();

        // PORTAL DO CIDADAO (M8): porta de LEITURA cidada (Contracts). Read-only por DOCUMENTO resolvido
        // server-side pelo modulo Cidadao — meus lancamentos em aberto, minha divida ativa e 2a via de DAM
        // (com revalidacao de titularidade anti-IDOR). Nao expoe entidade interna de Tributos.
        services.AddScoped<Tensorroot.Gov.Modules.Tributos.Contracts.IConsultaTributariaCidadao, PortalCidadao.ConsultaTributariaCidadao>();

        // Protesto extrajudicial (Lei 9.492/97) — ACL versionada por CRA. Simulado em dev/testes; o adapter
        // de produção (leiaute oficial CRA-RS, HttpClient + Polly) entra por configuração quando obtido o
        // convênio. // TODO(validar-oficial): leiaute/endpoint do CRA-RS.
        var protestoProvider = configuration["Protesto:Provider"] ?? "Simulado";
        if (string.Equals(protestoProvider, "CraRs", StringComparison.OrdinalIgnoreCase))
        {
            // TODO(validar-oficial): registrar AdnProtestoCraGateway com HttpClient resiliente quando houver leiaute oficial.
            services.AddSingleton<IProtestoCraGateway, SimuladoProtestoCraGateway>();
        }
        else
        {
            services.AddSingleton<IProtestoCraGateway, SimuladoProtestoCraGateway>();
        }

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
        IssItbiEndpoints.Map(endpoints);
        TaxasCosipAlvaraMelhoriaEndpoints.Map(endpoints);
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
