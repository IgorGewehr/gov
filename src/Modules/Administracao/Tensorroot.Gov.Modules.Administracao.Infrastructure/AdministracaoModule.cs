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
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Tempo;
using Tensorroot.Gov.Modules.Administracao.Application;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.Modules.Administracao.Infrastructure.Pncp;
using Tensorroot.Gov.Modules.Administracao.Infrastructure.Receita;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure;

/// <summary>
/// Modulo Administracao: registra o DbContext (provider por configuracao), interceptors, repositorios,
/// handlers (MediatR) e validators; expoe os endpoints. Ativavel por tenant via descoberta no ApiHost.
/// </summary>
public sealed class AdministracaoModule : IModule
{
    /// <inheritdoc />
    public string Name => "Administracao";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<AdministracaoDbContext>((serviceProvider, options) =>
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

        services.AddScoped<ILicitacaoRepository, LicitacaoRepository>();
        services.AddScoped<IContratoRepository, ContratoRepository>();
        services.AddScoped<IFornecedorRepository, FornecedorRepository>();

        // Anti-Corruption Layer (Receita Federal): consulta de CNPJ resiliente em producao; simulada para dev/testes.
        services.AddSingleton<IReceitaCnpjGateway, SimuladoReceitaCnpjGateway>();

        // === W9.1 — PNCP (Lei 14.133/2021, art. 94; gestao Dec. 10.764/2021) ===
        // Parametros de prazo do PNCP por tenant (sem numero magico — §16): defaults legais sobrescrititiveis.
        services.Configure<PncpOptions>(configuration.GetSection(PncpOptions.SecaoConfig));
        services.AddScoped<IPncpParametros, PncpParametros>();

        // ACL do PNCP. M9: impl. SIMULADA (numero deterministico, testavel contra WireMock).
        // TODO(M10): trocar por PncpGatewayHttp (login JWT ~1h + pre-cadastro + Polly + creds no Key Vault).
        services.AddScoped<IPncpGateway, PncpGatewaySimulado>();

        // Porta de leitura cross-module para a INVARIANTE DE BLOQUEIO (Financas checa o contrato antes do
        // empenho). Registrada aqui; no ApiHost e exposta a Financas via escopo dedicado (guarda H5).
        services.AddScoped<IConsultaContratoParaEmpenho, ConsultaContratoParaEmpenho>();

        var applicationAssembly = typeof(AbrirLicitacaoCommand).Assembly;
        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => AdministracaoEndpoints.Map(endpoints);

    /// <inheritdoc />
    public async Task MigrarBancoAsync(string connectionString, string provider, Guid tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<AdministracaoDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        await using var contexto = new AdministracaoDbContext(construtor.Options, SistemaTenantContext.Instancia);
        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DrenarOutboxAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var contexto = serviceProvider.GetRequiredService<AdministracaoDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }
}
