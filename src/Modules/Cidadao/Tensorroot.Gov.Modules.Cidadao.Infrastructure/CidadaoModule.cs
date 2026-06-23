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
using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;
using Tensorroot.Gov.Modules.Cidadao.Application.Autenticacao;
using Tensorroot.Gov.Modules.Cidadao.Application.Internal;
using Tensorroot.Gov.Modules.Cidadao.Infrastructure.GovBr;
using Tensorroot.Gov.Modules.Cidadao.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Cidadao.Infrastructure.Persistence.Repositories;
using Tensorroot.Gov.Modules.Cidadao.Infrastructure.Seguranca;

namespace Tensorroot.Gov.Modules.Cidadao.Infrastructure;

/// <summary>
/// Modulo Cidadao (Portal do Cidadao — M8): realm EXTERNO do cidadao. Registra o DbContext (schema
/// "cidadao", banco DEDICADO do tenant), interceptors, o repositorio da conta-cidadao, o hasher BCrypt
/// e o emissor de JWT do cidadao (claim tipo=cidadao, SEM RBAC), o resolvedor dado-proprio (ancora a
/// prova de bala) e a porta gov.br (stub // TODO(M10-creds)); expoe os endpoints sob /api/cidadao. E
/// SEGURANCA CRITICA: o cidadao e ator externo e so enxerga o que e dele.
/// </summary>
public sealed class CidadaoModule : IModule
{
    /// <inheritdoc />
    public string Name => "Cidadao";

    /// <inheritdoc />
    public void AddModule(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["Database:Provider"] ?? "Sqlite";

        services.AddDbContext<CidadaoDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<ITenantConnectionResolver>().ResolveConnectionString();
            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }

            options.AddModuleSaveChangesInterceptors(serviceProvider);
        });

        services.AddScoped<ICidadaoContaRepository, CidadaoContaRepository>();

        // ANCORA dado-proprio (espelho do Minha Folha): resolve a pessoa do PROPRIO cidadao pelo "sub".
        services.AddScoped<IResolvedorPessoaDoCidadaoAutenticado, ResolvedorPessoaDoCidadaoAutenticado>();

        // Seguranca do realm externo: hash BCrypt + emissao de JWT (tipo=cidadao, sem "perm").
        services.Configure<JwtCidadaoOptions>(configuration.GetSection(JwtCidadaoOptions.SecaoConfiguracao));
        var workFactor = configuration.GetValue<int?>("Seguranca:BCryptWorkFactor") ?? SenhaHasherCidadao.WorkFactorPadrao;
        services.AddSingleton<ISenhaHasherCidadao>(new SenhaHasherCidadao(workFactor));
        services.AddSingleton<IEmissorTokenCidadao, EmissorTokenCidadao>();

        // gov.br (Relying Party OIDC) — STUB ate creds + adesao do municipio (// TODO(M10-creds)).
        services.AddSingleton<IProvedorIdentidadeGovBr, ProvedorIdentidadeGovBrStub>();

        // Policy do portal (tipo=cidadao) — distinta do RBAC; gateia /api/cidadao/meus-*.
        services.AddAuthorizationBuilder().AddPortalCidadaoPolicy();

        var applicationAssembly = typeof(AutenticarCidadaoCommand).Assembly;
        services.AddMediatR(mediatr => mediatr.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => CidadaoEndpoints.Map(endpoints);

    /// <inheritdoc />
    public async Task MigrarBancoAsync(string connectionString, string provider, Guid tenantId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var ehSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        var construtor = new DbContextOptionsBuilder<CidadaoDbContext>();
        if (ehSqlServer)
        {
            construtor.UseSqlServer(connectionString);
        }
        else
        {
            construtor.UseSqlite(connectionString);
        }

        await using var contexto = new CidadaoDbContext(construtor.Options, SistemaTenantContext.Instancia);
        await SchemaProvisioner.AplicarAsync(contexto, ehSqlServer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DrenarOutboxAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var contexto = serviceProvider.GetRequiredService<CidadaoDbContext>();
        var publicador = serviceProvider.GetRequiredService<IOutboxPublisher>();
        await publicador.PublicarPendentesAsync(contexto, lote: 100, cancellationToken).ConfigureAwait(false);
    }
}
