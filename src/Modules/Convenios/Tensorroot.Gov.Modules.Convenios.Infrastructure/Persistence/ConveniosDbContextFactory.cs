using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Convenios.Infrastructure.Persistence;

/// <summary>
/// Fabrica de design-time do <see cref="ConveniosDbContext"/>, usada pelo <c>dotnet ef</c> para gerar/aplicar
/// migrations contra SQL Server (a string e apenas de design-time).
/// </summary>
public sealed class ConveniosDbContextFactory : IDesignTimeDbContextFactory<ConveniosDbContext>
{
    /// <inheritdoc />
    public ConveniosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ConveniosDbContext>()
            .UseSqlServer("Server=localhost;Database=TensorrootGov;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new ConveniosDbContext(options, new TenantDesignTime());
    }

    private sealed class TenantDesignTime : ITenantContext
    {
        public Guid TenantId => Guid.Empty;

        public bool HasTenant => false;
    }
}
