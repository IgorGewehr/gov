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
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Demonstracoes;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Motor;
using Tensorroot.Gov.Modules.Financas.Application.Empenhos;
using Tensorroot.Gov.Modules.Financas.Application.Planejamento.Compatibilidade;
using Tensorroot.Gov.Modules.Financas.Application.Planejamento.Lrf;
using Tensorroot.Gov.Modules.Financas.Application.RestosAPagar;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.ReadModels;
using Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure;

/// <summary>
/// Módulo Finanças: registra o DbContext (provider por configuração), interceptors,
/// repositórios, handlers e validators; expõe os endpoints. Ativável por tenant.
/// </summary>
public sealed class FinancasModule : IModule
{
    /// <inheritdoc />
    public string Name => "Financas";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<FinancasDbContext>((serviceProvider, options) =>
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

        services.AddScoped<IDotacaoOrcamentariaRepository, DotacaoOrcamentariaRepository>();
        services.AddScoped<IEmpenhoRepository, EmpenhoRepository>();

        // Planejamento orcamentario (PPA/LDO/LOA + creditos adicionais).
        services.AddScoped<IPpaRepository, PpaRepository>();
        services.AddScoped<ILdoRepository, LdoRepository>();
        services.AddScoped<ILoaRepository, LoaRepository>();
        services.AddScoped<ICreditoAdicionalRepository, CreditoAdicionalRepository>();
        services.AddScoped<ICompatibilidadeOrcamentariaService, CompatibilidadeOrcamentariaService>();

        // Opcoes LRF (obrigatoriedade de anexos AMF/ARF) parametrizaveis por tenant/config.
        var opcoesLrf = new OpcoesPlanejamentoLrf();
        configuration.GetSection(OpcoesPlanejamentoLrf.SecaoConfig).Bind(opcoesLrf);
        services.AddSingleton(opcoesLrf);

        // Opcoes de prazo (decadencia) de cancelamento de RAP, parametrizaveis por tenant/config.
        var opcoesRestosAPagar = new OpcoesRestosAPagar();
        configuration.GetSection(OpcoesRestosAPagar.SecaoConfig).Bind(opcoesRestosAPagar);
        services.AddSingleton(opcoesRestosAPagar);
        services.AddScoped<ILiquidacaoRepository, LiquidacaoRepository>();
        services.AddScoped<IOrdemDePagamentoRepository, OrdemDePagamentoRepository>();
        services.AddScoped<IRestoAPagarRepository, RestoAPagarRepository>();
        services.AddScoped<IReceitaArrecadadaRepository, ReceitaArrecadadaRepository>();

        // Tesouraria (caixa-banco): contas, movimentos, transferências, boletim, conciliação manual.
        services.AddScoped<IContaFinanceiraRepository, ContaFinanceiraRepository>();

        // Credores/fornecedores: cadastro + extrato consolidado (empenhos/liquidações/pagamentos).
        services.AddScoped<ICredorRepository, CredorRepository>();
        services.AddScoped<ICredorExtratoConsulta, CredorExtratoConsulta>();

        // Retenções/consignações (IRRF IN RFB 1234/2012, INSS, ISS, caução) + recolhimento extra-orçamentário.
        services.AddScoped<ITabelaIrrfServicosRepository, TabelaIrrfServicosRepository>();
        services.AddScoped<IGuiaRecolhimentoRepository, GuiaRecolhimentoRepository>();

        // Contabilidade (PCASP/MCASP).
        services.AddScoped<IContaContabilRepository, ContaContabilRepository>();
        services.AddScoped<ILancamentoContabilRepository, LancamentoContabilRepository>();
        services.AddScoped<ILancamentoContabilConsulta, LancamentoContabilConsulta>();
        services.AddScoped<IEventoContabilRepository, EventoContabilRepository>();
        services.AddScoped<IBalanceteProjection, BalanceteProjection>();
        services.AddScoped<IMscGeradaStore, MscGeradaStore>();
        services.AddScoped<MotorContabil>();

        // Encerramento de exercício (apuração patrimonial/orçamentária, RAP, transposição, abertura).
        services.AddScoped<IEncerramentoExercicioRepository, EncerramentoExercicioRepository>();
        services.AddScoped<IProjecaoSincronizador, ProjecaoSincronizador>();
        services.AddScoped<Application.Contabilidade.Encerramento.MotorEncerramento>();

        // MSC + demonstrações DCASP (read-models derivados do balancete).
        services.AddScoped<DemonstrativoContexto>();

        var applicationAssembly = typeof(EmpenharCommand).Assembly;
        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => FinancasEndpoints.Map(endpoints);

    /// <inheritdoc />
    public async Task MigrarBancoAsync(string connectionString, string provider, Guid tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<FinancasDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        await using var contexto = new FinancasDbContext(construtor.Options, SistemaTenantContext.Instancia);
        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DrenarOutboxAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var contexto = serviceProvider.GetRequiredService<FinancasDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }
}
