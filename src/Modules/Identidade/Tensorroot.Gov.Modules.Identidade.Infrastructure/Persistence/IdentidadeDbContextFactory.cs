using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Persistence;

/// <summary>
/// Fabrica de design-time do <see cref="IdentidadeDbContext"/>, usada pelo <c>dotnet ef</c>
/// para gerar/aplicar migrations contra SQL Server (a string e apenas de design-time).
/// </summary>
public sealed class IdentidadeDbContextFactory : IDesignTimeDbContextFactory<IdentidadeDbContext>
{
    /// <inheritdoc />
    public IdentidadeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentidadeDbContext>()
            .UseSqlServer("Server=localhost;Database=TensorrootGov;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new IdentidadeDbContext(options, new TenantDesignTime());
    }

    private sealed class TenantDesignTime : ITenantContext
    {
        public Guid TenantId => Guid.Empty;

        public bool HasTenant => false;
    }
}
