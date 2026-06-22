using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.Workers.NfseSync;

/// <summary>Contexto de tenant mutável, definido por iteração no worker (sem HTTP/JWT).</summary>
internal sealed class WorkerTenantContext : ITenantContext
{
    private Guid? _tenantId;

    public Guid TenantId => _tenantId
        ?? throw new InvalidOperationException("Tenant não definido no escopo atual do worker.");

    public bool HasTenant => _tenantId.HasValue;

    public void Definir(Guid tenantId) => _tenantId = tenantId;
}

/// <summary>Identidade de sistema do worker (registrada na trilha de auditoria).</summary>
internal sealed class SistemaCurrentUser : ICurrentUser
{
    public string? UserId => "sistema:nfse-sync";

    public string? UserName => "NFS-e Sync Worker";

    public string? IpAddress => null;
}

/// <summary>Configuração de um tenant a sincronizar (seção "Nfse:Tenants").</summary>
internal sealed class TenantNfseConfig
{
    public Guid TenantId { get; set; }

    public IReadOnlyList<string> Cnpjs { get; set; } = [];
}
