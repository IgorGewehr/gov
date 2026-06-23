using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.Workers.PontoColetor;

/// <summary>Contexto de tenant mutavel, definido por iteracao no worker (sem HTTP/JWT).</summary>
internal sealed class WorkerTenantContext : ITenantContext
{
    private Guid? _tenantId;

    public Guid TenantId => _tenantId
        ?? throw new InvalidOperationException("Tenant nao definido no escopo atual do worker.");

    public bool HasTenant => _tenantId.HasValue;

    public void Definir(Guid tenantId) => _tenantId = tenantId;
}

/// <summary>Identidade de sistema do worker (registrada na trilha de auditoria).</summary>
internal sealed class SistemaCurrentUser : ICurrentUser
{
    public string? UserId => "sistema:ponto-coletor";

    public string? UserName => "Ponto Coletor Worker";

    public string? IpAddress => null;
}

/// <summary>Configuracao de um tenant cujos REPs o worker coleta (secao "Ponto:Tenants").</summary>
internal sealed class TenantPontoConfig
{
    public Guid TenantId { get; set; }
}
