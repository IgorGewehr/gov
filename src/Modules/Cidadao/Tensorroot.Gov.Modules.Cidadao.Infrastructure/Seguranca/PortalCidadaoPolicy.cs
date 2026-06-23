using Microsoft.AspNetCore.Authorization;

namespace Tensorroot.Gov.Modules.Cidadao.Infrastructure.Seguranca;

/// <summary>
/// Policy de autorizacao do Portal do Cidadao: exige um principal AUTENTICADO cuja claim
/// <c>tipo</c> seja <c>cidadao</c> (token emitido pelo realm externo). E DISTINTA das policies RBAC
/// por permissao ("perm"): o cidadao nao tem papel organizacional. Deny-by-default — um token interno
/// (servidor/admin) NAO satisfaz a policy do portal, e vice-versa, isolando os dois realms.
/// </summary>
public static class PortalCidadaoPolicy
{
    /// <summary>Nome canonico da policy do portal.</summary>
    public const string Nome = "PortalCidadao";

    /// <summary>Registra a policy <see cref="Nome"/> no container (chamado pelo modulo).</summary>
    /// <param name="builder">Builder de autorizacao do ASP.NET Core.</param>
    /// <returns>O proprio builder, para encadeamento.</returns>
    public static AuthorizationBuilder AddPortalCidadaoPolicy(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddPolicy(Nome, policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim(EmissorTokenCidadao.ClaimTipo, EmissorTokenCidadao.ValorTipoCidadao));
    }
}
