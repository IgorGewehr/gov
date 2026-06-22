using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Administracao.Tests;

/// <summary>
/// Base de testes de integracao do modulo Administracao sobre SQLite em memoria:
/// abre uma unica conexao por classe de teste (o schema vive enquanto a conexao estiver aberta)
/// e cria contextos com os interceptors de tenant e de auditoria, a imagem de PatrimonioTestBase.
/// </summary>
public abstract class AdministracaoTestBase : IDisposable
{
    /// <summary>Tenant A — usado como tenant principal dos cenarios.</summary>
    protected static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Tenant B — usado para provar o isolamento (Global Query Filter).</summary>
    protected static readonly Guid TenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly SqliteConnection _connection;

    /// <summary>Abre a conexao SQLite em memoria compartilhada pelos contextos do teste.</summary>
    protected AdministracaoTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    /// <summary>Cria um <see cref="AdministracaoDbContext"/> no contexto do tenant informado, com auditoria e filtro por tenant.</summary>
    /// <param name="tenantId">Tenant cujo escopo o contexto enxerga.</param>
    /// <returns>Contexto pronto (schema criado).</returns>
    protected AdministracaoDbContext CriarContexto(Guid tenantId)
    {
        var tenantContext = new TenantContextFake(tenantId);
        var options = new DbContextOptionsBuilder<AdministracaoDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new TenantSaveChangesInterceptor(tenantContext),
                new AuditSaveChangesInterceptor(new CurrentUserFake(), TimeProvider.System))
            .Options;

        var contexto = new AdministracaoDbContext(options, tenantContext);
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
