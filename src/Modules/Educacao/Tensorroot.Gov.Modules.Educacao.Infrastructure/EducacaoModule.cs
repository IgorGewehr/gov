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
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Application.Escolas;
using Tensorroot.Gov.Modules.Educacao.Application.Fiscal;
using Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;
using Tensorroot.Gov.Modules.Educacao.Infrastructure.Fiscal;
using Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Repositories;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure;

/// <summary>
/// Modulo Educacao: registra o DbContext (provider por configuracao), interceptors, repositorios,
/// handlers (MediatR) e validators; expoe os endpoints. Ativavel por tenant via descoberta no ApiHost.
/// </summary>
public sealed class EducacaoModule : IModule
{
    /// <inheritdoc />
    public string Name => "Educacao";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<EducacaoDbContext>((serviceProvider, options) =>
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

        services.AddScoped<IEscolaRepository, EscolaRepository>();
        services.AddScoped<IAlunoRepository, AlunoRepository>();
        services.AddScoped<IMatriculaRepository, MatriculaRepository>();
        services.AddScoped<IDiarioClasseRepository, DiarioClasseRepository>();
        services.AddScoped<ITurmaRepository, TurmaRepository>();

        // Read model do diario coletivo, boletim e historico escolar (sub-onda 3a) — projecao
        // intra-modulo sobre Turma/Matricula/Aluno/DiarioClasse, sem entidade nova.
        services.AddScoped<IDiarioTurmaReadModel, DiarioTurmaReadModel>();

        // Sub-onda 3b — Merenda (PNAE): cardapio + distribuicao/consumo (reusa ItemEstoque por Id).
        services.AddScoped<ICardapioRepository, CardapioRepository>();
        services.AddScoped<IDistribuicaoMerendaRepository, DistribuicaoMerendaRepository>();

        // Sub-onda 3b — Transporte (PNATE): rotas + alunos transportados (reusa Veiculo/Aluno/Matricula por Id).
        services.AddScoped<IRotaTransporteRepository, RotaTransporteRepository>();

        // Nucleo fiscal de Educacao (M7 E-1/E-2/E-3): classificacao MDE, FUNDEB por origem, 70% folha.
        services.AddScoped<IRegraClassificacaoMdeRepository, RegraClassificacaoMdeRepository>();
        services.AddScoped<IDistribuicaoFundebRepository, DistribuicaoFundebRepository>();
        services.AddScoped<ILinhaExecucaoEducacaoRepository, LinhaExecucaoEducacaoRepository>();
        services.AddScoped<IExecucaoEducacaoReadModel, ExecucaoEducacaoReadModel>();
        services.AddScoped<IRemuneracaoMagisterioReadModel, RemuneracaoMagisterioReadModel>();
        services.AddScoped<IParametroMdeProvider, ParametroMdeProvider>();
        services.AddScoped<IParametroFundebProvider, ParametroFundebProvider>();

        var applicationAssembly = typeof(CredenciarEscolaCommand).Assembly;
        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => EducacaoEndpoints.Map(endpoints);

    /// <inheritdoc />
    public async Task MigrarBancoAsync(string connectionString, string provider, Guid tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<EducacaoDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        await using var contexto = new EducacaoDbContext(construtor.Options, new SeedTenantContext(tenantId));
        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);
        await SemearFiscalAsync(contexto, tenantId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Semeia (idempotente, por tenant) as regras default de classificacao MDE (CF art. 212 / LDB arts.
    /// 70/71), o percentual minimo legal de 25% (MDE) e o piso de 70% do FUNDEB (EC 108/2020), versionados.
    /// NAO sobrescreve parametros ja definidos pelo tenant (a Lei Organica pode fixar maior) — so cria
    /// quando ainda nao ha nenhum. Espelha o SemearFiscalAsync da Saude.
    /// </summary>
    private static async Task SemearFiscalAsync(EducacaoDbContext contexto, Guid tenantId, CancellationToken cancellationToken)
    {
        var mudou = false;

        var jaTemRegras = await contexto.RegrasClassificacaoMde.AnyAsync(cancellationToken).ConfigureAwait(false);
        if (!jaTemRegras)
        {
            foreach (var regra in SeedRegrasMde.Gerar(tenantId))
            {
                await contexto.RegrasClassificacaoMde.AddAsync(regra, cancellationToken).ConfigureAwait(false);
            }

            mudou = true;
        }

        var jaTemMde = await contexto.ParametrosFiscaisEducacao
            .AnyAsync(p => p.Chave == ParametroFiscalEducacao.ChavePercentualMinimoMde, cancellationToken)
            .ConfigureAwait(false);
        if (!jaTemMde)
        {
            // Default legal 25% (CF art. 212); vigencia ancorada na edicao da LDB (reprodutivel).
            await contexto.ParametrosFiscaisEducacao.AddAsync(
                ParametroFiscalEducacao.Criar(tenantId, ParametroFiscalEducacao.ChavePercentualMinimoMde, new DateOnly(1996, 12, 20), ParametroMdeProvider.PercentualMinimoLegalMde),
                cancellationToken).ConfigureAwait(false);
            mudou = true;
        }

        var jaTemFundeb = await contexto.ParametrosFiscaisEducacao
            .AnyAsync(p => p.Chave == ParametroFiscalEducacao.ChavePisoRemuneracaoFundeb, cancellationToken)
            .ConfigureAwait(false);
        if (!jaTemFundeb)
        {
            // Default legal 70% (EC 108/2020, que elevou o piso de 60%); vigencia ancorada na promulgacao.
            await contexto.ParametrosFiscaisEducacao.AddAsync(
                ParametroFiscalEducacao.Criar(tenantId, ParametroFiscalEducacao.ChavePisoRemuneracaoFundeb, new DateOnly(2020, 8, 26), ParametroFundebProvider.PisoRemuneracaoLegalFundeb),
                cancellationToken).ConfigureAwait(false);
            mudou = true;
        }

        if (mudou)
        {
            await contexto.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
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
        var contexto = serviceProvider.GetRequiredService<EducacaoDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }
}
