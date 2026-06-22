using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Persistence;

/// <summary>
/// Fabrica de design-time do <see cref="CofreDbContext"/>, usada pelo <c>dotnet ef</c> para
/// gerar/aplicar migrations contra SQL Server (string apenas de design-time).
/// </summary>
public sealed class CofreDbContextFactory : IDesignTimeDbContextFactory<CofreDbContext>
{
    /// <inheritdoc />
    public CofreDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CofreDbContext>()
            .UseSqlServer("Server=localhost;Database=TensorrootGov;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new CofreDbContext(options, new TenantDesignTime());
    }

    private sealed class TenantDesignTime : ITenantContext
    {
        public Guid TenantId => Guid.Empty;

        public bool HasTenant => false;
    }
}
