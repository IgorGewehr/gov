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
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Application.PortalPublico;
using Tensorroot.Gov.Modules.Transparencia.Application.RemessasTce;
using Tensorroot.Gov.Modules.Transparencia.Domain.Esic;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Fiscal;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.PortalPublico;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure;

/// <summary>
/// Modulo Transparencia: registra o DbContext (provider por configuracao), interceptors, repositorios,
/// portas das integracoes (SIAPC/PAD, SICONFI, e-Validador, catalogo/calendario), handlers (MediatR) e
/// validators; expoe os endpoints. Ativavel por tenant via descoberta no ApiHost.
/// </summary>
public sealed class TransparenciaModule : IModule
{
    /// <inheritdoc />
    public string Name => "Transparencia";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<TransparenciaDbContext>((serviceProvider, options) =>
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

        services.AddScoped<IRemessaTceRepository, RemessaTceRepository>();
        services.AddScoped<IDeclaracaoFiscalRepository, DeclaracaoFiscalRepository>();

        // Ponte RH -> Transparencia: read model do resumo de folha (fonte da remessa de folha ao TCE-RS).
        services.AddScoped<IResumoFolhaTceRepository, ResumoFolhaTceRepository>();

        // Nucleo fiscal M7.0 (Saude 15% ASPS / Educacao 25% MDE): classificador setorial por funcao/fonte,
        // calendario federal, parecer de conselho, projecao de execucao e provedor de percentuais vigentes.
        services.AddScoped<IFonteRecursoVinculadoRepository, FonteRecursoVinculadoRepository>();
        services.AddScoped<ICalendarioFederalRepository, CalendarioFederalRepository>();
        services.AddScoped<IParecerConselhoRepository, ParecerConselhoRepository>();
        services.AddScoped<ILinhaExecucaoFiscalRepository, LinhaExecucaoFiscalRepository>();
        services.AddScoped<IExecucaoSetorialReadModel, ExecucaoSetorialReadModel>();
        services.AddScoped<IParametroMinimoProvider, ParametroMinimoProvider>();

        // Leitura dos itens consolidados (read model alimentado por Integration Events — I-13).
        services.AddScoped<IPublicacaoTransparenciaRepository, SimuladoPublicacaoTransparenciaRepository>();

        // === Onda 2 — PORTAL PUBLICO + e-SIC + DADOS ABERTOS ===
        // Resolver de tenant por slug publico (sem JWT) e configuracao do portal por tenant.
        services.AddScoped<ITenantPublicoResolver, TenantPublicoResolver>();
        services.AddScoped<IPortalPublicoConfigRepository, PortalPublicoConfigRepository>();
        // Read models publicos (projecao I-13) + leitura paginada read-only.
        services.AddScoped<IProjecaoPublicaRepository, ProjecaoPublicaRepository>();
        services.AddScoped<IConsultaPublicaRepository, ConsultaPublicaRepository>();
        // e-SIC (LAI): agregado + calendario de dias uteis parametrizavel (prazo calculado, nao digitado).
        services.AddScoped<IPedidoSicRepository, PedidoSicRepository>();
        services.AddSingleton<ICalendarioDiasUteis, CalendarioDiasUteisPadrao>();

        // Catalogo de leiautes (grade posicional versionada por exercicio, dirigida por dados) e calendario.
        services.AddSingleton<ILeiauteCatalogo, SimuladoLeiauteCatalogo>();
        services.AddSingleton<ICalendarioFiscal, SimuladoCalendarioFiscal>();

        // Pre-validacao LOCAL real (e-Validador/RDI): criticas estruturais que BLOQUEIAM.
        services.AddScoped<IEValidadorTce, EValidadorLocalSiapc>();

        // Empacotamento real do ZIP nomeado (NAO transmite — TCE-RS nao tem API de envio).
        services.AddSingleton<IEmpacotadorRemessaSiapc, EmpacotadorRemessaSiapc>();

        // Geracao real da MSC (CSV adaptado do XBRL-GL, zipado) para upload MANUAL no portal SICONFI.
        services.AddSingleton<IGeradorMsc, GeradorMscCsv>();

        // SICONFI permanece com a transmissao simulada da declaracao (operador registra protocolo; sem POST).
        services.AddSingleton<ISiconfiGateway, SimuladoSiconfiGateway>();

        // Reconciliacao via API de Dados Abertos do SICONFI (SOMENTE consulta), atras de ACL + Polly.
        var siconfiProvider = configuration["Transparencia:Siconfi:Provider"] ?? "Simulado";
        if (string.Equals(siconfiProvider, "DadosAbertos", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IConsultaSiconfi, ConsultaSiconfiHttp>(client =>
                    client.BaseAddress = new Uri(
                        configuration["Transparencia:Siconfi:BaseUrl"]
                        ?? "https://apidatalake.tesouro.gov.br/ords/siconfi/tt/"))
                .AddStandardResilienceHandler();
        }
        else
        {
            services.AddSingleton<IConsultaSiconfi, SimuladoConsultaSiconfi>();
        }

        var applicationAssembly = typeof(GerarRemessaTceCommand).Assembly;
        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => TransparenciaEndpoints.Map(endpoints);

    /// <inheritdoc />
    public async Task MigrarBancoAsync(string connectionString, string provider, Guid tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<TransparenciaDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        await using var contexto = new TransparenciaDbContext(construtor.Options, SistemaTenantContext.Instancia);
        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DrenarOutboxAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var contexto = serviceProvider.GetRequiredService<TransparenciaDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }
}
