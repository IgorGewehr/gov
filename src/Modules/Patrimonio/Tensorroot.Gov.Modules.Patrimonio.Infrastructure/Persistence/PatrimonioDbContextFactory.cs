using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence;

/// <summary>
/// Fábrica de design-time do <see cref="PatrimonioDbContext"/>, usada pelo <c>dotnet ef</c>
/// para gerar/aplicar migrations contra SQL Server (a string é apenas de design-time).
/// </summary>
public sealed class PatrimonioDbContextFactory : IDesignTimeDbContextFactory<PatrimonioDbContext>
{
    /// <inheritdoc />
    public PatrimonioDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PatrimonioDbContext>()
            .UseSqlServer("Server=localhost;Database=TensorrootGov;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new PatrimonioDbContext(options, new TenantDesignTime());
    }

    private sealed class TenantDesignTime : ITenantContext
    {
        public Guid TenantId => Guid.Empty;

        public bool HasTenant => false;
    }
}
