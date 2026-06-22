using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Authorization;

/// <summary>
/// Requisito de autorização por permissão RBAC: exige a presença de uma claim "perm" igual ao
/// escopo informado. Negar por padrão — a ausência da claim reprova. Compartilhado por TODOS os
/// módulos (vive em BuildingBlocks para não acoplar módulos entre si).
/// </summary>
public sealed class PermissaoRequirement(string permissao) : IAuthorizationRequirement
{
    /// <summary>Tipo da claim de permissão emitida no token.</summary>
    public const string ClaimPermissao = "perm";

    /// <summary>Escopo (permissão do catálogo canônico) exigido.</summary>
    public string Permissao { get; } = !string.IsNullOrWhiteSpace(permissao)
        ? permissao
        : throw new ArgumentException("Permissão obrigatória.", nameof(permissao));
}

/// <summary>Handler que aprova o requisito quando o principal possui a claim "perm" correspondente.</summary>
public sealed class PermissaoHandler : AuthorizationHandler<PermissaoRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissaoRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var possui = context.User.Claims.Any(claim =>
            string.Equals(claim.Type, PermissaoRequirement.ClaimPermissao, StringComparison.Ordinal)
            && string.Equals(claim.Value, requirement.Permissao, StringComparison.Ordinal));

        if (possui)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Provedor de políticas que materializa, sob demanda, uma política por permissão com o prefixo
/// <see cref="PrefixoPolitica"/>. Delega ao provedor padrão as políticas não prefixadas.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    /// <summary>Prefixo das políticas dinâmicas de permissão (ex.: "perm:tributos.ver").</summary>
    public const string PrefixoPolitica = "perm:";

    private readonly DefaultAuthorizationPolicyProvider _fallback;

    /// <summary>Cria o provedor encadeando o provedor padrão para políticas convencionais.</summary>
    /// <param name="opcoes">Opções de autorização.</param>
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> opcoes)
        => _fallback = new DefaultAuthorizationPolicyProvider(opcoes);

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!string.IsNullOrEmpty(policyName)
            && policyName.StartsWith(PrefixoPolitica, StringComparison.Ordinal))
        {
            var permissao = policyName[PrefixoPolitica.Length..];
            var politica = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissaoRequirement(permissao))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(politica);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    /// <summary>Monta o nome canônico da política para um escopo de permissão.</summary>
    /// <param name="permissao">Escopo do catálogo canônico.</param>
    /// <returns>Nome da política (prefixado).</returns>
    public static string NomePolitica(string permissao)
        => string.Create(CultureInfo.InvariantCulture, $"{PrefixoPolitica}{permissao}");
}
