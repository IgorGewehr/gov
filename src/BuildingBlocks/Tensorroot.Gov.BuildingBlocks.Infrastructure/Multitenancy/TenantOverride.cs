namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;

/// <summary>
/// Sobreposição de tenant por escopo (scoped). Trabalhos de segundo plano (provisionamento,
/// drenagem de Outbox, remessas) definem o tenant explicitamente; o <c>ITenantContext</c> do
/// host dá precedência a este valor sobre o JWT.
/// </summary>
public sealed class TenantOverride
{
    /// <summary>Tenant forçado para o escopo atual (nulo = usar o JWT/contexto HTTP).</summary>
    public Guid? TenantId { get; set; }
}
