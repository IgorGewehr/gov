using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Educacao.Tests;

/// <summary>
/// Base de testes de integracao do modulo Educacao sobre SQLite em memoria: abre uma unica conexao
/// por classe de teste (o schema vive enquanto a conexao estiver aberta) e cria contextos com os
/// interceptors de tenant e de auditoria, a imagem de PatrimonioTestBase. Usa o
/// <see cref="EducacaoDbContext"/> gerado pela fabrica.
/// </summary>
public abstract class EducacaoTestBase : IDisposable
{
    /// <summary>Tenant A — usado como tenant principal dos cenarios.</summary>
    protected static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Tenant B — usado para provar o isolamento (Global Query Filter).</summary>
    protected static readonly Guid TenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly SqliteConnection _connection;

    /// <summary>Abre a conexao SQLite em memoria compartilhada pelos contextos do teste.</summary>
    protected EducacaoTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    /// <summary>Cria um <see cref="EducacaoDbContext"/> no contexto do tenant informado, com auditoria e filtro por tenant.</summary>
    /// <param name="tenantId">Tenant cujo escopo o contexto enxerga.</param>
    /// <returns>Contexto pronto (schema criado).</returns>
    protected EducacaoDbContext CriarContexto(Guid tenantId)
    {
        var tenantContext = new TenantContextFake(tenantId);
        var options = new DbContextOptionsBuilder<EducacaoDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFake(), TimeProvider.System))
            .Options;

        var contexto = new EducacaoDbContext(options, tenantContext);
        contexto.Database.EnsureCreated();
        return contexto;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}

internal sealed class TenantContextFake(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;

    public bool HasTenant => true;
}

internal sealed class CurrentUserFake : ICurrentUser
{
    public string? UserId => "teste";

    public string? UserName => "Usuario de Teste";

    public string? IpAddress => "127.0.0.1";
}
