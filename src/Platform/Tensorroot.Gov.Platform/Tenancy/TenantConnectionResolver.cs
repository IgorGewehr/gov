using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Platform.Persistence;

namespace Tensorroot.Gov.Platform.Tenancy;

/// <summary>
/// Cache em memória (singleton) das conexões dedicadas por tenant, com TTL curto e caminho de
/// INVALIDAÇÃO explícito. O TTL limita a janela em que uma conexão obsoleta (após rotação de
/// segredo/failover/migração de banco) pode ser servida; a invalidação a fecha imediatamente
/// quando o fluxo de provisionamento/rotação avisa o cache.
/// </summary>
public sealed class TenantConnectionCache(TimeProvider timeProvider) : ITenantConnectionCacheInvalidator
{
    /// <summary>
    /// TTL padrão do cache de conexão. Curto de propósito: limita a divergência entre instâncias
    /// após rotação/failover quando o caminho de invalidação não foi (ou não pôde ser) acionado.
    /// </summary>
    public static readonly TimeSpan TtlPadrao = TimeSpan.FromMinutes(5);

    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ConcurrentDictionary<Guid, Entrada> _mapa = new();

    /// <summary>TTL efetivo das entradas (configurável; padrão <see cref="TtlPadrao"/>).</summary>
    public TimeSpan Ttl { get; init; } = TtlPadrao;

    /// <summary>Quantidade de entradas vivas no cache (inclui expiradas ainda não evictadas).</summary>
    public int Contagem => _mapa.Count;

    /// <summary>
    /// Retorna a conexão cacheada do tenant, recalculando-a via <paramref name="fabrica"/> quando
    /// ausente OU expirada pelo TTL. Atômico por tenant (não há rebusca concorrente duplicada
    /// para a mesma chave dentro da janela de validade).
    /// </summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="fabrica">Função que busca a conexão no catálogo quando o cache não serve.</param>
    /// <returns>Connection string vigente do tenant.</returns>
    public string ObterOuAdicionar(Guid tenantId, Func<Guid, string> fabrica)
    {
        ArgumentNullException.ThrowIfNull(fabrica);

        var agora = _timeProvider.GetUtcNow();
        var entrada = _mapa.AddOrUpdate(
            tenantId,
            id => new Entrada(fabrica(id), agora + Ttl),
            (id, existente) => existente.ExpiraEmUtc > agora
                ? existente
                : new Entrada(fabrica(id), agora + Ttl));

        return entrada.ConnectionString;
    }

    /// <inheritdoc />
    public bool Invalidar(Guid tenantId) => _mapa.TryRemove(tenantId, out _);

    /// <inheritdoc />
    public void InvalidarTodos() => _mapa.Clear();

    private readonly record struct Entrada(string ConnectionString, DateTimeOffset ExpiraEmUtc);
}

/// <summary>
/// Resolve a conexão dedicada do tenant atual a partir do catálogo da plataforma,
/// com cache em memória (TTL curto + invalidação). Fallback de desenvolvimento: um arquivo
/// SQLite por tenant.
/// </summary>
public sealed class TenantConnectionResolver(
    ITenantContext tenantContext,
    PlatformDbContext platform,
    TenantConnectionCache cache) : ITenantConnectionResolver
{
    /// <inheritdoc />
    public string ResolveConnectionString()
    {
        if (!tenantContext.HasTenant)
        {
            throw new InvalidOperationException("Não há tenant no contexto para resolver a conexão dedicada.");
        }

        var tenantId = tenantContext.TenantId;
        return cache.ObterOuAdicionar(tenantId, id =>
        {
            var conexao = platform.Tenants
                .Where(tenant => tenant.Id == id)
                .Select(tenant => tenant.ConnectionString)
                .FirstOrDefault();

            return string.IsNullOrWhiteSpace(conexao)
                ? ConexaoPadrao(id)
                : conexao;
        });
    }

    /// <summary>Conexão padrão (convenção de desenvolvimento) para um tenant sem conexão dedicada definida.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <returns>Connection string SQLite dedicada por tenant.</returns>
    public static string ConexaoPadrao(Guid tenantId)
        => string.Create(CultureInfo.InvariantCulture, $"Data Source=tenant_{tenantId:N}.db");
}
