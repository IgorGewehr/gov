namespace Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Resolve a string de conexão do banco DEDICADO do tenant atual (database-per-tenant).
/// Cada ente público possui seu próprio banco — isolamento físico exigido por auditorias
/// de dinheiro público e por editais. O DB central da plataforma guarda o catálogo.
/// </summary>
public interface ITenantConnectionResolver
{
    /// <summary>Retorna a connection string do banco dedicado do tenant corrente.</summary>
    /// <returns>String de conexão.</returns>
    /// <exception cref="InvalidOperationException">Quando não há tenant no contexto.</exception>
    string ResolveConnectionString();
}

/// <summary>
/// Invalida o cache de connection string de um tenant. DEVE ser chamado pelo fluxo de
/// provisionamento/rotação de segredo/failover de banco, para que a próxima resolução
/// re-busque a conexão no catálogo da plataforma em vez de servir a string obsoleta.
/// </summary>
public interface ITenantConnectionCacheInvalidator
{
    /// <summary>Remove a entrada cacheada do tenant; a próxima resolução re-busca no catálogo.</summary>
    /// <param name="tenantId">Tenant cuja conexão foi rotacionada/alterada.</param>
    /// <returns>Verdadeiro se havia uma entrada cacheada que foi removida.</returns>
    bool Invalidar(Guid tenantId);

    /// <summary>Invalida TODAS as entradas (ex.: rotação em massa/failover de cluster).</summary>
    void InvalidarTodos();
}
