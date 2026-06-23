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
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Integracoes;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Fiscal;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Integracoes;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Lookups;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Parametros;
using Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Repositories;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure;

/// <summary>
/// Modulo AssistenciaSocial: registra o DbContext (provider por configuracao), interceptors,
/// repositorios, handlers (MediatR) e validators; expoe os endpoints. Ativavel por tenant via
/// descoberta no ApiHost.
/// </summary>
public sealed class AssistenciaSocialModule : IModule
{
    /// <inheritdoc />
    public string Name => "AssistenciaSocial";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<AssistenciaSocialDbContext>((serviceProvider, options) =>
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

        services.AddScoped<IFamiliaRepository, FamiliaRepository>();
        services.AddScoped<IBeneficioRepository, BeneficioRepository>();
        services.AddScoped<IProntuarioSuasRepository, ProntuarioSuasRepository>();

        // A-0: criterio MUNICIPAL do beneficio eventual (sem teto federal de 1/4 SM revogado).
        services.AddScoped<ICriterioBeneficioEventualProvider, CriterioBeneficioEventualProvider>();

        // A-1 (FMAS por bloco/piso) + A-2 (RMA consolidado do prontuario).
        services.AddScoped<IFundoMunicipalAssistenciaRepository, FundoMunicipalAssistenciaRepository>();
        services.AddScoped<IRegistroMensalAtendimentoRepository, RegistroMensalAtendimentoRepository>();
        services.AddScoped<IConsolidacaoRmaReadModel, ConsolidacaoRmaReadModel>();

        // Read models de apoio (CRAS/CREAS e parametros vigentes) — tenant-scoped.
        services.AddScoped<UnidadeAtendimentoLookupRepository>();
        services.AddScoped<IUnidadeAtendimentoRepository>(sp => sp.GetRequiredService<UnidadeAtendimentoLookupRepository>());
        services.AddScoped<IUnidadeAtendimentoTipoLookup>(sp => sp.GetRequiredService<UnidadeAtendimentoLookupRepository>());
        services.AddScoped<IParametroVigenteProvider, ParametroVigenteProvider>();

        // ACL CadUnico/MDS (somente leitura — I-9): simulado para dev/testes ate a integracao
        // concreta (HttpClient tipado + Polly) ser implementada nesta camada.
        services.AddSingleton<SimuladoCadUnicoGateway>();
        services.AddSingleton<ICadUnicoGateway>(serviceProvider => serviceProvider.GetRequiredService<SimuladoCadUnicoGateway>());
        services.AddSingleton<ICadUnicoReadModel>(serviceProvider => serviceProvider.GetRequiredService<SimuladoCadUnicoGateway>());

        var applicationAssembly = typeof(ReferenciarFamiliaCommand).Assembly;
        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => AssistenciaSocialEndpoints.Map(endpoints);

    /// <inheritdoc />
    public async Task MigrarBancoAsync(string connectionString, string provider, Guid tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<AssistenciaSocialDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        await using var contexto = new AssistenciaSocialDbContext(construtor.Options, new SeedTenantContext(tenantId));
        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);
        await SemearCriteriosBeneficioEventualAsync(contexto, tenantId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// <b>A-0:</b> semeia (idempotente, por tenant) um EXEMPLO PARAMETRIZAVEL dos criterios MUNICIPAIS de
    /// beneficio eventual — NUNCA o teto federal de 1/4 SM (revogado pela Lei 12.435/2011). Os valores aqui
    /// sao placeholders do tenant piloto; o municipio sobrescreve por lei/decreto + CMAS. So cria quando o
    /// tenant ainda nao tem nenhum criterio (nao sobrescreve o que ja foi parametrizado).
    /// // TODO(validar-oficial): substituir pelos criterios reais da lei municipal de beneficios eventuais
    /// de Maximiliano de Almeida/RS (modalidades habilitadas, valores e cortes de renda) + resolucao do CMAS.
    /// </summary>
    private static async Task SemearCriteriosBeneficioEventualAsync(AssistenciaSocialDbContext contexto, Guid tenantId, CancellationToken cancellationToken)
    {
        var jaTemCriterios = await contexto.CriteriosBeneficioEventual.AnyAsync(cancellationToken).ConfigureAwait(false);
        if (jaTemCriterios)
        {
            return;
        }

        // Vigencia ancorada na Lei 12.435/2011 (que retirou o teto fixo da LOAS) — reprodutivel.
        var vigencia = new DateOnly(2011, 7, 6);

        // Exemplo municipal (placeholder, parametrizavel): natalidade/morte sem corte de renda;
        // vulnerabilidade temporaria/calamidade com corte de renda definido pela lei municipal (1/2 SM aqui,
        // NAO o revogado 1/4 SM — e apenas exemplo, o municipio define).
        var criterios = new[]
        {
            CriterioBeneficioEventualMunicipal.Criar(tenantId, ModalidadeBeneficioEventual.Natalidade, vigencia, multiploRendaSalarioMinimo: null),
            CriterioBeneficioEventualMunicipal.Criar(tenantId, ModalidadeBeneficioEventual.Morte, vigencia, multiploRendaSalarioMinimo: null),
            CriterioBeneficioEventualMunicipal.Criar(tenantId, ModalidadeBeneficioEventual.VulnerabilidadeTemporaria, vigencia, multiploRendaSalarioMinimo: 0.5m),
            CriterioBeneficioEventualMunicipal.Criar(tenantId, ModalidadeBeneficioEventual.Calamidade, vigencia, multiploRendaSalarioMinimo: null),
        };

        foreach (var criterio in criterios)
        {
            await contexto.CriteriosBeneficioEventual.AddAsync(criterio, cancellationToken).ConfigureAwait(false);
        }

        await contexto.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Contexto de tenant fixo usado apenas na semeadura/migracao (carimba o TenantId do banco do tenant).</summary>
    private sealed class SeedTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;

        public bool HasTenant => tenantId != Guid.Empty;
    }

    /// <inheritdoc />
    public async Task DrenarOutboxAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var contexto = serviceProvider.GetRequiredService<AssistenciaSocialDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }
}
