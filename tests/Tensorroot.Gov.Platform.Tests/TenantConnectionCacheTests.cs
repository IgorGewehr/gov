using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Platform.Persistence;
using Tensorroot.Gov.Platform.Tenancy;
using Xunit;

namespace Tensorroot.Gov.Platform.Tests;

/// <summary>
/// Achado K6: o cache de connection string DEVE ter caminho de invalidação. Sem ele, após rotação de
/// segredo/failover/migração de banco a instância continuaria servindo a conexão antiga até reiniciar
/// (instâncias divergem, apontando para banco obsoleto que move verba pública).
/// </summary>
public sealed class TenantConnectionCacheTests
{
    [Fact]
    public void Invalidar_remove_a_entrada_e_proxima_resolucao_rebusca()
    {
        var cache = new TenantConnectionCache(TimeProvider.System);
        var tenantId = Guid.NewGuid();
        var buscas = 0;

        string Fabrica(Guid _)
        {
            buscas++;
            return $"Data Source=v{buscas}.db";
        }

        // Primeira resolução: busca e cacheia.
        cache.ObterOuAdicionar(tenantId, Fabrica).Should().Be("Data Source=v1.db");
        buscas.Should().Be(1);

        // Sem invalidar: serve do cache (não re-busca).
        cache.ObterOuAdicionar(tenantId, Fabrica).Should().Be("Data Source=v1.db");
        buscas.Should().Be(1);

        // Invalidação remove a entrada.
        cache.Invalidar(tenantId).Should().BeTrue();
        cache.Contagem.Should().Be(0);

        // Próxima resolução RE-BUSCA (pega o novo valor após a rotação).
        cache.ObterOuAdicionar(tenantId, Fabrica).Should().Be("Data Source=v2.db");
        buscas.Should().Be(2);
    }

    [Fact]
    public void Invalidar_inexistente_retorna_falso()
        => new TenantConnectionCache(TimeProvider.System).Invalidar(Guid.NewGuid()).Should().BeFalse();

    [Fact]
    public void Ttl_expira_a_entrada_e_forca_rebusca()
    {
        var tempo = new TempoFake(DateTimeOffset.UnixEpoch);
        var cache = new TenantConnectionCache(tempo) { Ttl = TimeSpan.FromMinutes(5) };
        var tenantId = Guid.NewGuid();
        var buscas = 0;
        string Fabrica(Guid _) => $"Data Source=v{++buscas}.db";

        cache.ObterOuAdicionar(tenantId, Fabrica).Should().Be("Data Source=v1.db");

        // Dentro do TTL: serve do cache.
        tempo.Avancar(TimeSpan.FromMinutes(4));
        cache.ObterOuAdicionar(tenantId, Fabrica).Should().Be("Data Source=v1.db");
        buscas.Should().Be(1);

        // Após o TTL: re-busca.
        tempo.Avancar(TimeSpan.FromMinutes(2));
        cache.ObterOuAdicionar(tenantId, Fabrica).Should().Be("Data Source=v2.db");
        buscas.Should().Be(2);
    }

    [Fact]
    public async Task Rotacao_de_conexao_invalida_o_cache_e_resolver_serve_a_nova_string()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseSqlite(conexao).Options;
        await using var contexto = new PlatformDbContext(options);
        await contexto.Database.EnsureCreatedAsync();

        var cache = new TenantConnectionCache(TimeProvider.System);
        var protetor = ProvedorKekFake.Protetor();
        var provisionamento = new TenantProvisioningService(contexto, cache, protetor);

        var tenantId = await provisionamento.ProvisionarAsync(
            "11.222.333/0001-81", "Prefeitura de Exemplo", PoderTenant.Executivo,
            connectionString: "Data Source=antigo.db",
            new[] { "Tributos" }, CancellationToken.None);

        var tenantContext = new TenantContextFake(tenantId);
        var resolver = new TenantConnectionResolver(tenantContext, contexto, cache, protetor);

        // Resolve e cacheia a conexão antiga (DECIFRADA do envelope em repouso — SEC-1).
        resolver.ResolveConnectionString().Should().Be("Data Source=antigo.db");

        // Rotaciona: persiste a nova (CIFRADA) e INVALIDA o cache.
        await provisionamento.RotacionarConexaoAsync(tenantId, "Data Source=novo.db", CancellationToken.None);

        // A próxima resolução serve a NOVA conexão (não a obsoleta).
        resolver.ResolveConnectionString().Should().Be("Data Source=novo.db");
    }

    private sealed class TenantContextFake(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;

        public bool HasTenant => true;
    }

    private sealed class TempoFake(DateTimeOffset inicio) : TimeProvider
    {
        private DateTimeOffset _agora = inicio;

        public override DateTimeOffset GetUtcNow() => _agora;

        public void Avancar(TimeSpan delta) => _agora += delta;
    }
}
