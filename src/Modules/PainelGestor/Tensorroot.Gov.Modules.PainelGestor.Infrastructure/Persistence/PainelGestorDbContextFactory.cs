using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.Modules.PainelGestor.Infrastructure.Persistence;

/// <summary>
/// Fábrica de design-time do <see cref="PainelGestorDbContext"/>, usada pelo <c>dotnet ef</c> para
/// gerar/aplicar migrations contra SQL Server (a string é apenas de design-time).
/// </summary>
public sealed class PainelGestorDbContextFactory : IDesignTimeDbContextFactory<PainelGestorDbContext>
{
    /// <inheritdoc />
    public PainelGestorDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PainelGestorDbContext>()
            .UseSqlServer("Server=localhost;Database=TensorrootGov;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new PainelGestorDbContext(options, new TenantDesignTime());
    }

    private sealed class TenantDesignTime : ITenantContext
    {
        public Guid TenantId => Guid.Empty;

        public bool HasTenant => false;
    }
}
