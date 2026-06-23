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
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _travas = new();

    /// <summary>TTL efetivo das entradas (configurável; padrão <see cref="TtlPadrao"/>).</summary>
    public TimeSpan Ttl { get; init; } = TtlPadrao;

    /// <summary>Quantidade de entradas vivas no cache (inclui expiradas ainda não evictadas).</summary>
    public int Contagem => _mapa.Count;

    /// <summary>
    /// Tenta servir a conexão do tenant SÓ a partir do cache, SEM jamais tocar a rede/Key Vault.
    /// É o que o HOT PATH síncrono (factory de <c>DbContext</c>) usa: quando o cache está quente
    /// (aquecido async pelo pipeline), a resolução de conexão NUNCA bloqueia uma thread em I/O.
    /// </summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="connectionString">Connection string vigente, se houver entrada válida no cache.</param>
    /// <returns><c>true</c> se havia entrada NÃO EXPIRADA; <c>false</c> em cache frio/expirado.</returns>
    public bool TentarObter(Guid tenantId, out string connectionString)
    {
        if (_mapa.TryGetValue(tenantId, out var entrada) && entrada.ExpiraEmUtc > _timeProvider.GetUtcNow())
        {
            connectionString = entrada.ConnectionString;
            return true;
        }

        connectionString = string.Empty;
        return false;
    }

    /// <summary>
    /// Retorna a conexão cacheada do tenant, recalculando-a via <paramref name="fabrica"/> quando
    /// ausente OU expirada pelo TTL. Atômico por tenant (não há rebusca concorrente duplicada
    /// para a mesma chave dentro da janela de validade). Use SÓ com fábricas NÃO-BLOQUEANTES
    /// (catálogo local / valor já em claro); o caminho que envolve Key Vault é o
    /// <see cref="ObterOuAdicionarAsync"/>.
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

    /// <summary>
    /// Resolve a conexão do tenant de forma ASSÍNCRONA — o ÚNICO caminho que pode tocar o Key Vault.
    /// Em cache quente devolve imediatamente; em cache frio/expirado decifra via <paramref name="fabricaAsync"/>
    /// com <c>await</c> (sem bloquear thread do pool) e popula o cache. Concorrências para o MESMO tenant
    /// compartilham UMA decifragem (dedup por <see cref="SemaphoreSlim"/>), evitando rajada ao Key Vault.
    /// </summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="fabricaAsync">Decifragem/resolução assíncrona da conexão (pode bater no Key Vault).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Connection string vigente do tenant.</returns>
    public async Task<string> ObterOuAdicionarAsync(Guid tenantId, Func<Guid, CancellationToken, Task<string>> fabricaAsync, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fabricaAsync);

        if (TentarObter(tenantId, out var emCache))
        {
            return emCache;
        }

        // Dedup por tenant: só UMA decifragem por chave de cada vez (não martela o Key Vault sob rajada).
        var trava = _travas.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));
        await trava.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Outra requisição pode ter populado enquanto esperávamos a trava.
            if (TentarObter(tenantId, out emCache))
            {
                return emCache;
            }

            var conexao = await fabricaAsync(tenantId, cancellationToken).ConfigureAwait(false);
            _mapa[tenantId] = new Entrada(conexao, _timeProvider.GetUtcNow() + Ttl);
            return conexao;
        }
        finally
        {
            trava.Release();
        }
    }

    /// <inheritdoc />
    public bool Invalidar(Guid tenantId) => _mapa.TryRemove(tenantId, out _);

    /// <inheritdoc />
    public void InvalidarTodos() => _mapa.Clear();

    private readonly record struct Entrada(string ConnectionString, DateTimeOffset ExpiraEmUtc);
}

/// <summary>
/// Resolve a conexão dedicada do tenant atual a partir do catálogo da plataforma,
/// com cache em memória (TTL curto + invalidação). A connection string é guardada CIFRADA em
/// repouso (SEC-1, envelope AES-256-GCM + KEK) e DECIFRADA só em memória aqui; o cache guarda
/// apenas o valor em claro já resolvido (curto TTL, processo único). Fallback de desenvolvimento:
/// um arquivo SQLite por tenant.
/// </summary>
public sealed class TenantConnectionResolver(
    ITenantContext tenantContext,
    PlatformDbContext platform,
    TenantConnectionCache cache,
    ProtetorConexaoTenant protetor) : ITenantConnectionResolver
{
    /// <inheritdoc />
    public string ResolveConnectionString()
    {
        if (!tenantContext.HasTenant)
        {
            throw new InvalidOperationException("Não há tenant no contexto para resolver a conexão dedicada.");
        }

        var tenantId = tenantContext.TenantId;

        // HOT PATH (P0-2): em PROD o cache é AQUECIDO de forma ASSÍNCRONA pelo pipeline (AquecerAsync,
        // chamado no middleware por requisição) ANTES de qualquer factory de DbContext rodar. Aqui só
        // lemos o cache — ZERO I/O de rede/Key Vault na thread da requisição → sem thread-pool starvation.
        if (cache.TentarObter(tenantId, out var emCache))
        {
            return emCache;
        }

        // Cache frio: resolve a partir do catálogo (consulta LOCAL, sem rede externa).
        var conexao = ConexaoBruta(tenantId);

        // Valor já EM CLARO (dev SQLite por convenção ou string legada): não há decifragem a fazer,
        // resolução totalmente síncrona e sem rede — seguro popular o cache aqui mesmo.
        if (!ProtetorConexaoTenant.EstaProtegida(conexao))
        {
            return cache.ObterOuAdicionar(tenantId, _ => conexao);
        }

        // Valor PROTEGIDO com cache frio: a decifragem TEM de ser assíncrona (Key Vault em PROD). Isso
        // só ocorre se o aquecimento async do pipeline não rodou para esta requisição (ex.: caminho
        // não-HTTP/worker). Delegamos ao caminho assíncrono dedicado, que decifra com await e dedup —
        // continua SEM .Result/.Wait bloqueante numa thread do pool de requisições do hot path HTTP.
        return cache
            .ObterOuAdicionarAsync(tenantId, (id, ct) => protetor.RevelarAsync(id, conexao, ct), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// AQUECIMENTO assíncrono do cache de conexão do tenant atual — chamado pelo pipeline (middleware)
    /// no início da requisição. Decifra o envelope protegido (Key Vault em PROD) com <c>await</c>,
    /// FORA do factory síncrono do EF, populando o cache para que o hot path em
    /// <see cref="ResolveConnectionString"/> NUNCA bloqueie uma thread em I/O de rede (P0-2).
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    public async Task AquecerAsync(CancellationToken cancellationToken)
    {
        if (!tenantContext.HasTenant)
        {
            return;
        }

        var tenantId = tenantContext.TenantId;
        if (cache.TentarObter(tenantId, out _))
        {
            return;
        }

        var conexao = ConexaoBruta(tenantId);
        await cache.ObterOuAdicionarAsync(
            tenantId,
            async (id, ct) => ProtetorConexaoTenant.EstaProtegida(conexao)
                ? await protetor.RevelarAsync(id, conexao, ct).ConfigureAwait(false)
                : conexao,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lê a connection string BRUTA (possivelmente cifrada) do catálogo de controle, ou o fallback DEV.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <returns>Valor persistido no catálogo (cifrado/em claro) ou a conexão padrão de desenvolvimento.</returns>
    private string ConexaoBruta(Guid tenantId)
    {
        var conexao = platform.Tenants
            .Where(tenant => tenant.Id == tenantId)
            .Select(tenant => tenant.ConnectionString)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(conexao) ? ConexaoPadrao(tenantId) : conexao;
    }

    /// <summary>Conexão padrão (convenção de desenvolvimento) para um tenant sem conexão dedicada definida.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <returns>Connection string SQLite dedicada por tenant.</returns>
    public static string ConexaoPadrao(Guid tenantId)
        => string.Create(CultureInfo.InvariantCulture, $"Data Source=tenant_{tenantId:N}.db");
}
