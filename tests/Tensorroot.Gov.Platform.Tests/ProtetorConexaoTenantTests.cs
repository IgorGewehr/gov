using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Platform.Persistence;
using Tensorroot.Gov.Platform.Tenancy;
using Xunit;

namespace Tensorroot.Gov.Platform.Tests;

/// <summary>
/// SEC-1 (RED-TEAM): a connection string do banco DEDICADO do tenant NUNCA pode ficar em CLARO no
/// catálogo de CONTROLE. Estes testes provam o envelope encryption (AES-256-GCM + KEK embrulhada,
/// AAD do tenant): nada em claro persistido, decifragem só em memória e integridade autenticada
/// (adulteração/cross-tenant faz a decifragem FALHAR).
/// </summary>
public sealed class ProtetorConexaoTenantTests
{
    private const string Conexao = "Server=prod-sql-01;Database=tenant_x;User Id=svc;Password=S3nh@Sup3rSecreta!;";

    [Fact]
    public async Task Protege_e_revela_round_trip_preservando_a_connection_string()
    {
        var protetor = ProvedorKekFake.Protetor();
        var tenantId = Guid.NewGuid();

        var envelope = await protetor.ProtegerAsync(tenantId, Conexao, CancellationToken.None);

        envelope.Should().StartWith(ProtetorConexaoTenant.Prefixo);
        ProtetorConexaoTenant.EstaProtegida(envelope).Should().BeTrue();

        var revelada = await protetor.RevelarAsync(tenantId, envelope, CancellationToken.None);
        revelada.Should().Be(Conexao);
    }

    [Fact]
    public async Task Envelope_nao_contem_a_senha_em_claro()
    {
        var protetor = ProvedorKekFake.Protetor();
        var envelope = await protetor.ProtegerAsync(Guid.NewGuid(), Conexao, CancellationToken.None);

        // Nenhum trecho sensível da connection string sobrevive em claro no blob persistido.
        envelope.Should().NotContain("S3nh@Sup3rSecreta!");
        envelope.Should().NotContain("Password");
        envelope.Should().NotContain("prod-sql-01");
    }

    [Fact]
    public async Task Decifrar_com_tenant_diferente_falha_por_integridade_AAD()
    {
        var protetor = ProvedorKekFake.Protetor();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var envelope = await protetor.ProtegerAsync(tenantA, Conexao, CancellationToken.None);

        // AAD = TenantId: tentar revelar como OUTRO tenant quebra a tag GCM → LANÇA (cross-tenant).
        var acao = async () => await protetor.RevelarAsync(tenantB, envelope, CancellationToken.None);
        await acao.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Decifrar_envelope_adulterado_falha()
    {
        var protetor = ProvedorKekFake.Protetor();
        var tenantId = Guid.NewGuid();
        var envelope = await protetor.ProtegerAsync(tenantId, Conexao, CancellationToken.None);

        // Vira um bit no corpo Base64 do envelope.
        var corpo = Convert.FromBase64String(envelope[ProtetorConexaoTenant.Prefixo.Length..]);
        corpo[^1] ^= 0xFF;
        var adulterado = ProtetorConexaoTenant.Prefixo + Convert.ToBase64String(corpo);

        var acao = async () => await protetor.RevelarAsync(tenantId, adulterado, CancellationToken.None);
        await acao.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public static void Valor_legado_em_claro_nao_e_reconhecido_como_protegido()
    {
        ProtetorConexaoTenant.EstaProtegida("Data Source=tenant.db").Should().BeFalse();
        ProtetorConexaoTenant.EstaProtegida(null).Should().BeFalse();
    }

    [Fact]
    public async Task Connection_string_persistida_no_catalogo_fica_CIFRADA_em_repouso()
    {
        using var conexao = new SqliteConnection("DataSource=:memory:");
        conexao.Open();
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseSqlite(conexao).Options;
        await using var contexto = new PlatformDbContext(options);
        await contexto.Database.EnsureCreatedAsync();

        var protetor = ProvedorKekFake.Protetor();
        var provisionamento = new TenantProvisioningService(
            contexto, new TenantConnectionCache(TimeProvider.System), protetor);

        var tenantId = await provisionamento.ProvisionarAsync(
            "11.222.333/0001-81", "Prefeitura de Exemplo", PoderTenant.Executivo,
            connectionString: Conexao, new[] { "Tributos" }, CancellationToken.None);

        // A coluna do catálogo NÃO contém a string em claro — está no formato de envelope (SEC-1).
        var persistida = contexto.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.ConnectionString)
            .Single();

        persistida.Should().StartWith(ProtetorConexaoTenant.Prefixo);
        persistida.Should().NotContain("S3nh@Sup3rSecreta!");

        // O resolver decifra só em memória e devolve a conexão real.
        var resolver = new TenantConnectionResolver(
            new TenantContextSec1Fake(tenantId), contexto, new TenantConnectionCache(TimeProvider.System), protetor);
        resolver.ResolveConnectionString().Should().Be(Conexao);
    }

    private sealed class TenantContextSec1Fake(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;

        public bool HasTenant => true;
    }
}
