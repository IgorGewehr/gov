using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence;

/// <summary>
/// Fabrica de design-time do <see cref="TransparenciaDbContext"/>, usada pelo <c>dotnet ef</c>
/// para gerar/aplicar migrations contra SQL Server (a string e apenas de design-time).
/// </summary>
public sealed class TransparenciaDbContextFactory : IDesignTimeDbContextFactory<TransparenciaDbContext>
{
    /// <inheritdoc />
    public TransparenciaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TransparenciaDbContext>()
            .UseSqlServer("Server=localhost;Database=TensorrootGov;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new TransparenciaDbContext(options, new TenantDesignTime());
    }

    private sealed class TenantDesignTime : ITenantContext
    {
        public Guid TenantId => Guid.Empty;

        public bool HasTenant => false;
    }
}
