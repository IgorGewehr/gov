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
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Consignacoes;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Internal;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto.Coleta;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Providers;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Ponto.Coleta;
using Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Previdencia;

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

        // PARIDADE-PoC Trilha A (SW-A7/A8): PORTARIAS / atos de pessoal (numeracao sequencial por
        // exercicio/tenant) e PASEP (apuracao da base = folha bruta; transmissao = M10). Reajuste
        // salarial em lote reusa o ICargoRepository e o Cargo.AlterarVencimento (sem repositorio novo).
        services.AddScoped<IPortariaRepository, PortariaRepository>();
        services.AddScoped<IApuracaoPasepRepository, ApuracaoPasepRepository>();

        // AUTOSSERVICO ("Minha Folha"): repositorio do vinculo usuario<->servidor + a ancora que
        // resolve o servidor do PROPRIO usuario autenticado (ABAC dado-proprio a prova de bala).
        services.AddScoped<IVinculoServidorUsuarioRepository, VinculoServidorUsuarioRepository>();
        services.AddScoped<IResolvedorServidorDoUsuarioAutenticado, ResolvedorServidorDoUsuarioAutenticado>();

        services.AddScoped<IFolhaDePagamentoRepository, FolhaDePagamentoRepository>();
        services.AddScoped<IRubricaFolhaRepository, RubricaFolhaRepository>();

        // RELATORIOS GERENCIAIS (Onda 3a — ONDA3-DESIGN §4.1): consulta read-side que projeta sobre a
        // folha/servidores/cargos existentes (folha por secretaria/UO e fonte, evolucao da despesa de
        // pessoal, mapa de cargos, demonstrativo TCE). Somente leitura — sem dominio novo.
        services.AddScoped<IRelatoriosFolhaConsulta, RelatoriosFolhaConsulta>();

        // AFASTAMENTOS TIPADOS (Onda 1 — efeito na folha): repositorio do agregado, provider de regras
        // (catalogo persistido por tenant + defaults legais parametrizados) e o ajustador que aplica o
        // efeito ao provento-base no lancamento de eventos (gancho da folha — design RH §3.3).
        services.AddScoped<IAfastamentoRepository, AfastamentoRepository>();
        services.AddScoped<IRegraAfastamentoProvider, RegraAfastamentoProvider>();
        services.AddScoped<AjustadorProventoPorAfastamento>();

        // CONSIGNACOES + MARGEM CONSIGNAVEL (Onda 2 — Lei 14.131/2021): cadastro mestre de consignatarias,
        // catalogo de rubricas consignaveis, contratos de consignacao e percentuais de margem por vigencia.
        // A CalculadoraMargemConsignavel e a fonte unica da margem (consulta/averbacao/gancho); o
        // LancadorDescontosConsignados aplica o desconto na folha respeitando a margem (corte por prioridade),
        // mantendo o MotorDeCalculoFolha puro (mesma porta do AjustadorProventoPorAfastamento — design RH §2.5).
        services.AddScoped<IConsignatariaRepository, ConsignatariaRepository>();
        services.AddScoped<IRubricaConsignavelRepository, RubricaConsignavelRepository>();
        services.AddScoped<IContratoConsignacaoRepository, ContratoConsignacaoRepository>();
        services.AddScoped<IParametrosMargemProvider, ParametrosMargemProvider>();
        services.AddScoped<IBaseConsignavelProvider, BaseConsignavelProvider>();
        services.AddScoped<CalculadoraMargemConsignavel>();
        services.AddScoped<LancadorDescontosConsignados>();
        services.AddScoped<ITabelasLegaisRepository, TabelasLegaisRepository>();
        services.AddScoped<IServidorRegimeConsulta, ServidorRegimeConsulta>();
        services.AddScoped<IRubricaS1010Consulta, RubricaS1010Consulta>();
        services.AddScoped<IParametrosFolhaProvider, ParametrosFolhaProvider>();
        services.AddScoped<ITabelasLegaisProvider, TabelasLegaisProvider>();
        services.AddScoped<IMarcacaoPontoRepository, MarcacaoPontoRepository>();
        services.AddScoped<IJornadaTrabalhoRepository, JornadaTrabalhoRepository>();
        services.AddScoped<IApuracaoPontoRepository, ApuracaoPontoRepository>();

        // BANCO DE HORAS (saldo vivo por servidor): livro-razao de creditos/debitos/prescricao alimentado
        // pelo fechamento das apuracoes de ponto (gancho via evento ApuracaoPontoFechada) e por ajustes
        // manuais; rotina de prescricao respeita a janela parametrizavel (ParametrosPonto).
        services.AddScoped<IBancoDeHorasRepository, BancoDeHorasRepository>();
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

        // SST / SAUDE OCUPACIONAL (paridade SAPI + eSocial SST): ASO (S-2220/PCMSO), exposicao a agentes
        // nocivos (S-2240/PPP) e CAT (S-2210). Repositorios dos tres agregados; a geracao dos eventos reusa
        // o GeradorEventoApplicationService (idempotencia/Outbox) e o ACL GeradorEventosSst.
        services.AddScoped<IExameOcupacionalRepository, ExameOcupacionalRepository>();
        services.AddScoped<IExposicaoAgenteNocivoRepository, ExposicaoAgenteNocivoRepository>();
        services.AddScoped<IComunicacaoAcidenteRepository, ComunicacaoAcidenteRepository>();

        // PC1: regime previdenciario do quadro = parametro do tenant (tem RPPS proprio?), nao roteamento
        // fixo. Default RGPS/INSS (municipio pequeno sem RPPS proprio — Maximiliano de Almeida).
        services.AddScoped<IPoliticaPrevidenciariaProvider, PoliticaPrevidenciariaProvider>();
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
