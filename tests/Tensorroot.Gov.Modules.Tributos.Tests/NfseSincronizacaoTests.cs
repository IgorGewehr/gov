using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Nfse;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Tensorroot.Gov.Modules.Tributos.Tests;

/// <summary>
/// Prova da Fase 5: a sincronização NFS-e/ADN importa notas (gateway simulado),
/// deduplica por chave de acesso na reexecução e respeita o isolamento por tenant.
/// </summary>
public sealed class NfseSincronizacaoTests : IDisposable
{
    private static readonly Guid TenantA = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly SqliteConnection _connection;

    public NfseSincronizacaoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task Sincronizacao_importa_notas_deduplica_na_reexecucao_e_isola_tenant()
    {
        var cnpjs = new[] { "11222333000181" };
        var desde = new DateOnly(2026, 6, 1);

        // 1ª execução: o gateway simulado devolve 2 notas por CNPJ.
        await using (var contexto = CriarContexto(TenantA))
        {
            var importadas = await CriarSincronizador(contexto, TenantA).SincronizarAsync(cnpjs, desde, CancellationToken.None);
            importadas.Should().Be(2);
        }

        // 2ª execução: deduplicação por chave de acesso → nenhuma nova.
        await using (var contexto = CriarContexto(TenantA))
        {
            var importadas = await CriarSincronizador(contexto, TenantA).SincronizarAsync(cnpjs, desde, CancellationToken.None);
            importadas.Should().Be(0);
        }

        // Estado persistido para o Tenant A.
        await using (var contexto = CriarContexto(TenantA))
        {
            (await contexto.NotasFiscaisServico.ToListAsync()).Should().HaveCount(2);
        }

        // Outro tenant não enxerga as notas (Global Query Filter).
        await using (var contexto = CriarContexto(Guid.NewGuid()))
        {
            (await contexto.NotasFiscaisServico.ToListAsync()).Should().BeEmpty();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    private TributosDbContext CriarContexto(Guid tenantId)
    {
        var tenant = new TenantContextFake(tenantId);
        var options = new DbContextOptionsBuilder<TributosDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenant),
                new AuditSaveChangesInterceptor(new CurrentUserFake(), TimeProvider.System))
            .Options;

        var contexto = new TributosDbContext(options, tenant);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    private static NfseSincronizador CriarSincronizador(TributosDbContext contexto, Guid tenantId)
        => new(
            new SimuladoNfseGateway(),
            new NotaFiscalServicoRepository(contexto),
            contexto,
            new TenantContextFake(tenantId));
}

file sealed class TenantContextFake(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

file sealed class CurrentUserFake : ICurrentUser
{
    public string? UserId => "teste";

    public string? UserName => "Usuário de Teste";

    public string? IpAddress => "127.0.0.1";
}
