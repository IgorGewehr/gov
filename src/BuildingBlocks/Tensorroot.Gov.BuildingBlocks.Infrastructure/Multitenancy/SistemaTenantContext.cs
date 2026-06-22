using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;

/// <summary>
/// Contexto de tenant "de sistema" (sem tenant resolvido). Usado apenas para construir um
/// DbContext em operações de DDL/migração, onde o filtro por tenant não é avaliado.
/// </summary>
public sealed class SistemaTenantContext : ITenantContext
{
    /// <summary>Instância única reutilizável.</summary>
    public static SistemaTenantContext Instancia { get; } = new();

    /// <inheritdoc />
    public Guid TenantId => Guid.Empty;

    /// <inheritdoc />
    public bool HasTenant => false;
}
