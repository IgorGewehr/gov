using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence;

/// <summary>
/// Fabrica de design-time do <see cref="RecursosHumanosDbContext"/>, usada pelo <c>dotnet ef</c>
/// para gerar/aplicar migrations contra SQL Server (a string e apenas de design-time).
/// </summary>
public sealed class RecursosHumanosDbContextFactory : IDesignTimeDbContextFactory<RecursosHumanosDbContext>
{
    /// <inheritdoc />
    public RecursosHumanosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RecursosHumanosDbContext>()
            .UseSqlServer("Server=localhost;Database=TensorrootGov;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new RecursosHumanosDbContext(options, new TenantDesignTime());
    }

    private sealed class TenantDesignTime : ITenantContext
    {
        public Guid TenantId => Guid.Empty;

        public bool HasTenant => false;
    }
}
