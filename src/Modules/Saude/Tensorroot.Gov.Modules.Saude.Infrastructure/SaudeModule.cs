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
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Fiscal;
using Tensorroot.Gov.Modules.Saude.Application.Pacientes;
using Tensorroot.Gov.Modules.Saude.Domain.Fiscal;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Fiscal;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Integracoes;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Repositories;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure;

/// <summary>
/// Modulo Saude: registra o DbContext (provider por configuracao), interceptors, repositorios,
/// gateways/ACLs (CADSUS, CNES, RNDS, SISAB, SISREG, ICP-Brasil), handlers (MediatR) e validators;
/// expoe os endpoints. Ativavel por tenant via descoberta no ApiHost.
/// </summary>
public sealed class SaudeModule : IModule
{
    /// <inheritdoc />
    public string Name => "Saude";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<SaudeDbContext>((serviceProvider, options) =>
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

        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<IAtendimentoRepository, AtendimentoRepository>();
        services.AddScoped<ISolicitacaoRegulacaoRepository, SolicitacaoRegulacaoRepository>();

        // Cadastros-mestres locais (Onda 1 profundidade): estabelecimento (CNES) e profissional (CBO/CNES).
        services.AddScoped<IEstabelecimentoCadastroRepository, EstabelecimentoCadastroRepository>();
        services.AddScoped<IProfissionalCadastroRepository, ProfissionalCadastroRepository>();

        // Agendamento (Onda 2 profundidade): agenda/vagas, marcacao e fila de espera (reusa Paciente/UBS/Profissional).
        services.AddScoped<IAgendaProfissionalRepository, AgendaProfissionalRepository>();
        services.AddScoped<IAgendamentoRepository, AgendamentoRepository>();
        services.AddScoped<IFilaEsperaRepository, FilaEsperaRepository>();

        // Farmacia/Dispensacao (Onda 3c-1): catalogo + estoque por lote/FEFO + dispensacao ao paciente (LGPD).
        services.AddScoped<IMedicamentoRepository, MedicamentoRepository>();
        services.AddScoped<IEstoqueMedicamentoRepository, EstoqueMedicamentoRepository>();
        services.AddScoped<IDispensacaoRepository, DispensacaoRepository>();

        // Imunizacao (Onda 3c-1): catalogo de imunobiologicos + carteira/aprazamento (reusa estoque de Farmacia).
        services.AddScoped<IImunobiologicoRepository, ImunobiologicoRepository>();
        services.AddScoped<ICarteiraVacinacaoRepository, CarteiraVacinacaoRepository>();

        // Gateways/ACLs governamentais: implementacao simulada para dev/testes. Em producao,
        // HTTP resiliente (Polly) atras de Anti-Corruption Layer, com certificados no Azure Key Vault.
        services.AddScoped<ICadsusGateway, SimuladoCadsusGateway>();

        // ACL CNES agora consulta os AGREGADOS LOCAIS reais (substitui o SimuladoEstabelecimentoRepository
        // que so checava Guid.Empty). // TODO(prod: CNES oficial quando houver credencial).
        services.AddScoped<IEstabelecimentoRepository, EstabelecimentoRepository>();
        services.AddScoped<IRndsGateway, SimuladoRndsGateway>();
        services.AddScoped<ISisabGateway, SimuladoSisabGateway>();
        services.AddScoped<ISisregGateway, SimuladoSisregGateway>();
        services.AddScoped<IAssinaturaIcpBrasilService, SimuladoAssinaturaIcpBrasilService>();
        services.AddScoped<ICotaRepository, SimuladoCotaRepository>();

        // Nucleo fiscal de Saude (M7 S-1/S-2): classificacao ASPS, FMS por bloco, execucao e percentuais.
        services.AddScoped<IRegraClassificacaoAspsRepository, RegraClassificacaoAspsRepository>();
        services.AddScoped<IFundoMunicipalSaudeRepository, FundoMunicipalSaudeRepository>();
        services.AddScoped<ILinhaExecucaoSaudeRepository, LinhaExecucaoSaudeRepository>();
        services.AddScoped<IExecucaoSaudeReadModel, ExecucaoSaudeReadModel>();
        services.AddScoped<IParametroAspsProvider, ParametroAspsProvider>();

        var applicationAssembly = typeof(CadastrarPacienteCommand).Assembly;
        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => SaudeEndpoints.Map(endpoints);

    /// <inheritdoc />
    public async Task MigrarBancoAsync(string connectionString, string provider, Guid tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<SaudeDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        await using var contexto = new SaudeDbContext(construtor.Options, new SeedTenantContext(tenantId));
        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);
        await SemearFiscalAsync(contexto, tenantId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Semeia (idempotente, por tenant) as regras default de classificacao ASPS (LC 141/2012 arts. 3º/4º)
    /// e o percentual minimo legal de 15% versionado. NAO sobrescreve parametros ja definidos pelo tenant
    /// (a Lei Organica pode fixar maior) — so cria quando ainda nao ha nenhum.
    /// </summary>
    private static async Task SemearFiscalAsync(SaudeDbContext contexto, Guid tenantId, CancellationToken cancellationToken)
    {
        var jaTemRegras = await contexto.RegrasClassificacaoAsps.AnyAsync(cancellationToken).ConfigureAwait(false);
        if (!jaTemRegras)
        {
            foreach (var regra in SeedRegrasAsps.Gerar(tenantId))
            {
                await contexto.RegrasClassificacaoAsps.AddAsync(regra, cancellationToken).ConfigureAwait(false);
            }
        }

        var jaTemPercentual = await contexto.ParametrosFiscaisSaude
            .AnyAsync(p => p.Chave == ParametroFiscalSaude.ChavePercentualMinimoAsps, cancellationToken)
            .ConfigureAwait(false);
        if (!jaTemPercentual)
        {
            // Default legal 15% (LC 141/2012, art. 7º); vigencia ancorada na edicao da lei (reprodutivel).
            await contexto.ParametrosFiscaisSaude.AddAsync(
                ParametroFiscalSaude.Criar(tenantId, ParametroFiscalSaude.ChavePercentualMinimoAsps, new DateOnly(2012, 1, 16), ParametroAspsProvider.PercentualMinimoLegalAsps),
                cancellationToken).ConfigureAwait(false);
        }

        if (!jaTemRegras || !jaTemPercentual)
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
        var contexto = serviceProvider.GetRequiredService<SaudeDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }
}
