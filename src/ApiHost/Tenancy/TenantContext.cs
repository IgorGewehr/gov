using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;

namespace Tensorroot.Gov.ApiHost.Tenancy;

/// <summary>
/// Resolve o tenant atual: precedência ao override de escopo (jobs de segundo plano e
/// provisionamento); na ausência dele, a claim "tenant_id" do JWT.
/// </summary>
internal sealed class TenantContext(IHttpContextAccessor httpContextAccessor, TenantOverride tenantOverride) : ITenantContext
{
    public bool HasTenant => tenantOverride.TenantId is not null || TryResolve(out _);

    public Guid TenantId => tenantOverride.TenantId
        ?? (TryResolve(out var tenantId)
            ? tenantId
            : throw new InvalidOperationException("Nenhum tenant resolvido no contexto atual."));

    private bool TryResolve(out Guid tenantId)
    {
        tenantId = Guid.Empty;
        var value = httpContextAccessor.HttpContext?.User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(value, out tenantId);
    }
}
