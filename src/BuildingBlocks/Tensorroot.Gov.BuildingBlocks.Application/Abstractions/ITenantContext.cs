namespace Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Fornece o contexto do tenant (ente público) da requisição/execução atual,
/// resolvido a partir do JWT (no ApiHost) ou da iteração explícita (nos Workers).
/// </summary>
public interface ITenantContext
{
    /// <summary>Identificador do tenant atual.</summary>
    /// <exception cref="InvalidOperationException">Quando não há tenant resolvido.</exception>
    Guid TenantId { get; }

    /// <summary>Indica se há um tenant resolvido no contexto atual.</summary>
    bool HasTenant { get; }
}
