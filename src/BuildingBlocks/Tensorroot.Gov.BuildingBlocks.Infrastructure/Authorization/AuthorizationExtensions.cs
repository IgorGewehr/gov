using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;

/// <summary>Extensões de fiação do RBAC por permissão (claims "perm"). Compartilhadas por todos os módulos.</summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Registra o <see cref="PermissionPolicyProvider"/> (políticas dinâmicas por permissão) e o
    /// handler de permissão. Deve ser chamado pelo Composition Root (ApiHost).
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <returns>A própria coleção.</returns>
    public static IServiceCollection AddRbacPermissoes(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissaoHandler>();
        return services;
    }

    /// <summary>
    /// Exige a permissão RBAC informada (claim "perm") para acessar o endpoint/grupo. Negar por
    /// padrão: sem a claim correspondente, a requisição é reprovada (403).
    /// </summary>
    /// <typeparam name="TBuilder">Tipo do construtor de convenção de endpoint.</typeparam>
    /// <param name="builder">Construtor do endpoint ou grupo.</param>
    /// <param name="permissao">Escopo do catálogo canônico (ex.: "tributos.ver").</param>
    /// <returns>O próprio construtor, para encadeamento.</returns>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permissao)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissao);
        return builder.RequireAuthorization(PermissionPolicyProvider.NomePolitica(permissao));
    }
}
