using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Modularity;

/// <summary>
/// Contrato de um módulo (Bounded Context) plugável. Implementado pela camada
/// Infrastructure de cada módulo e descoberto/ativado pelo ApiHost conforme o
/// licenciamento do tenant.
/// </summary>
public interface IModule
{
    /// <summary>Nome único do módulo — corresponde à chave de licenciamento por tenant.</summary>
    string Name { get; }

    /// <summary>Registra os serviços do módulo (DbContext, repositórios, handlers) no contêiner.</summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    void AddModule(IServiceCollection services, IConfiguration configuration);

    /// <summary>Mapeia os endpoints HTTP expostos pelo módulo.</summary>
    /// <param name="endpoints">Construtor de rotas de endpoint.</param>
    void MapEndpoints(IEndpointRouteBuilder endpoints);

    /// <summary>
    /// Cria/atualiza o schema do módulo no banco DEDICADO informado (provisionamento de tenant) e,
    /// quando aplicável, semeia dados iniciais para o tenant (ex.: papel/usuário administrador na
    /// Identidade). No-op para módulos sem persistência própria.
    /// </summary>
    /// <param name="connectionString">Conexão do banco dedicado do tenant.</param>
    /// <param name="provider">Provider ("Sqlite" ou "SqlServer").</param>
    /// <param name="tenantId">Tenant que está sendo provisionado (para semeadura tenant-scoped).</param>
    /// <param name="serviceProvider">Provedor de serviços do escopo de provisionamento (para semeadura central).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task MigrarBancoAsync(
        string connectionString,
        string provider,
        Guid tenantId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Drena o Outbox do módulo (publica os eventos pendentes) para o tenant do escopo atual.
    /// No-op para módulos sem persistência própria.
    /// </summary>
    /// <param name="serviceProvider">Provedor de serviços do escopo (com o tenant já definido).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task DrenarOutboxAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
