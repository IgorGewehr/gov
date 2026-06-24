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
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Autenticacao;
using Tensorroot.Gov.Modules.Identidade.Application.Internal;
using Tensorroot.Gov.Modules.Identidade.Domain.Papeis;
using Tensorroot.Gov.Modules.Identidade.Domain.Permissoes;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Identidade.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Identidade.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.Modules.Identidade.Infrastructure.Seguranca;
using Tensorroot.Gov.Platform.Tenancy;

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure;

/// <summary>
/// Modulo Identidade (RBAC + autenticacao). Registra o DbContext (provider-aware) no banco
/// DEDICADO do tenant, interceptors, repositorios, o hasher BCrypt, o emissor de JWT e o
/// adaptador do indice central de login; expoe os endpoints e, ao provisionar um tenant, semeia
/// o papel "Administrador" (todas as permissoes) e um usuario administrador. E SEGURANCA CRITICA.
/// </summary>
public sealed class IdentidadeModule : IModule
{
    /// <summary>Nome do papel administrador semeado por tenant.</summary>
    public const string PapelAdministrador = "Administrador";

    private const string EmailAdminPadrao = "admin@tensorroot.gov";
    private const string SenhaAdminPadrao = "Mudar@123";

    // UO raiz semeada por tenant (parametrizavel/renomeavel depois — a estrutura real e definida
    // por lei municipal, MODELO §11.7). Serve de ancora de escopo no go-live.
    private const string CodigoRaizPadrao = "RAIZ";
    private const string NomeRaizPadrao = "Ente (raiz)";

    /// <inheritdoc />
    public string Name => "Identidade";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<IdentidadeDbContext>((serviceProvider, options) =>
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

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IPapelRepository, PapelRepository>();
        services.AddScoped<IUnidadeRepository, UnidadeRepository>();
        services.AddScoped<IRegistroLoginCentral, RegistroLoginCentral>();

        // Escopo de UO por requisicao (MODELO §5): resolve, do usuario atual, o conjunto de UOs que
        // ele pode LER; alimenta o filtro de leitura por UO do ModuleDbContext. Registrado UMA vez
        // (a Identidade e sempre carregada) e disponivel a TODOS os modulos que adotam o filtro.
        services.AddScoped<IResolvedorEscopoUnidade, ResolvedorEscopoUnidade>();
        services.AddScoped<ITenantUnidadeContext, TenantUnidadeContext>();

        // AA-5/D3: politica de (sub)delegacao por tenant (teto de profundidade) — parametrizavel.
        services.AddScoped<IPoliticaDelegacaoProvider, PoliticaDelegacaoProvider>();

        // W10.6 ID-1/ID-2: guarda de escopo/I4 dos comandos administrativos sobre a CONTA do usuario
        // (reset de senha, edicao de e-mail, ativar/desativar). Reusada pelos quatro handlers.
        services.AddScoped<AutorizacaoAdminUsuario>();

        // Seguranca: hash BCrypt e emissao de JWT HS256.
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SecaoConfiguracao));
        var workFactor = configuration.GetValue<int?>("Seguranca:BCryptWorkFactor") ?? SenhaHasher.WorkFactorPadrao;
        services.AddSingleton<ISenhaHasher>(new SenhaHasher(workFactor));
        services.AddSingleton<IEmissorToken, EmissorToken>();

        var applicationAssembly = typeof(AutenticarCommand).Assembly;
        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => IdentidadeEndpoints.Map(endpoints);

    /// <inheritdoc />
    public async Task MigrarBancoAsync(
        string connectionString,
        string provider,
        Guid tenantId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<IdentidadeDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        // Contexto fixado no tenant que esta sendo provisionado (semeadura tenant-scoped correta).
        var tenantContext = new ContextoTenantFixo(tenantId);
        await using var contexto = new IdentidadeDbContext(construtor.Options, tenantContext);

        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);

        await SemearAdministradorAsync(contexto, tenantId, serviceProvider, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DrenarOutboxAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var contexto = serviceProvider.GetRequiredService<IdentidadeDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }

    private static async Task SemearAdministradorAsync(
        IdentidadeDbContext contexto,
        Guid tenantId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var hasher = serviceProvider.GetRequiredService<ISenhaHasher>();

        var emailAdmin = configuration["Identidade:Admin:Email"] ?? EmailAdminPadrao;
        var senhaAdmin = configuration["Identidade:Admin:Senha"] ?? SenhaAdminPadrao;
        var email = Email.De(emailAdmin);

        // Idempotente: nao re-semeia se ja existir o papel/usuario administrador no tenant.
        var papelExistente = await contexto.Papeis
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(papel => papel.TenantId == tenantId && papel.Nome == PapelAdministrador, cancellationToken)
            .ConfigureAwait(false);

        var papelAdmin = papelExistente;
        if (papelAdmin is null)
        {
            // Papel Administrador com TODAS as permissoes do catalogo canonico.
            papelAdmin = Papel.Criar(tenantId, PapelAdministrador, Permissoes.Todas);
            contexto.Papeis.Add(papelAdmin);
        }

        // UO RAIZ do tenant (MODELO §10.2): ancora de escopo do go-live. O admin recebe o papel
        // Administrador nesta raiz com IncluiSubunidades=true → acesso GLOBAL preservado (e ele
        // proprio passa a satisfazer a regra I4 para conceder papeis a outros usuarios).
        var raizExistente = await contexto.Unidades
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(unidade => unidade.TenantId == tenantId && unidade.UnidadePaiId == null, cancellationToken)
            .ConfigureAwait(false);

        var raiz = raizExistente;
        if (raiz is null)
        {
            raiz = UnidadeOrganizacional.CriarRaiz(tenantId, CodigoRaizPadrao, NomeRaizPadrao, TipoUnidade.Gabinete);
            contexto.Unidades.Add(raiz);
        }

        // É necessário que a raiz exista (e tenha Id estável) antes da migração de dados abaixo.
        // Se a raiz acabou de ser criada nesta execução, persistimos para garantir o Id antes de
        // re-ancorar as atribuições legadas (idempotente: em re-execuções a raiz já existe).
        if (raizExistente is null)
        {
            await contexto.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // MIGRAÇÃO DE DADOS (MODELO §10.2), idempotente: converte cada par (Usuario, PapelId) legado
        // — materializado como AtribuicaoDePapel na UO sentinela RaizPendente (Guid.Empty) pela ponte
        // DefinirPapeis — numa atribuição na UO RAIZ REAL do tenant, com IncluiSubunidades=true e
        // Origem=Direta preservados. Garante que o acesso GLOBAL atual continue valendo no go-live e
        // que o escopo seja resolvível server-side (Guid.Empty não é UO persistida). Em tenants já
        // migrados, ReancorarAtribuicoesPendentesNaRaiz não encontra pendências e não altera nada.
        var usuariosDoTenant = await contexto.Usuarios
            .IgnoreQueryFilters()
            .Where(usuario => usuario.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var usuario in usuariosDoTenant)
        {
            usuario.ReancorarAtribuicoesPendentesNaRaiz(raiz.Id);
        }

        var usuarioExistente = usuariosDoTenant.Any(usuario => usuario.Email == email);

        if (!usuarioExistente)
        {
            var senhaHash = hasher.Hash(senhaAdmin);
            var admin = Usuario.Criar(tenantId, "Administrador", email, senhaHash);
            // Atribuicao COM ESCOPO na UO raiz real (e nao na sentinela RaizPendente): acesso global.
            admin.AtribuirPapel(
                papelAdmin.Id,
                raiz.Id,
                incluiSubunidades: true,
                Vigencia.Aberta(DateTimeOffset.UtcNow),
                OrigemAtribuicao.Direta());
            contexto.Usuarios.Add(admin);

            // Registra o login no indice central (email → tenant) no banco de CONTROLE.
            var indiceCentral = serviceProvider.GetRequiredService<IUsuarioTenantIndexService>();
            await indiceCentral.RegistrarAsync(email.Valor, tenantId, cancellationToken).ConfigureAwait(false);
        }

        await contexto.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Contexto de tenant fixo para semeadura durante o provisionamento.</summary>
    private sealed class ContextoTenantFixo(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;

        public bool HasTenant => true;
    }
}
