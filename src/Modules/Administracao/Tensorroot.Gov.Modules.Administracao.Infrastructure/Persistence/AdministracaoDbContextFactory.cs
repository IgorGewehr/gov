using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence;

/// <summary>
/// Fabrica de design-time do <see cref="AdministracaoDbContext"/>, usada pelo <c>dotnet ef</c>
/// para gerar/aplicar migrations contra SQL Server (a string e apenas de design-time).
/// </summary>
public sealed class AdministracaoDbContextFactory : IDesignTimeDbContextFactory<AdministracaoDbContext>
{
    /// <inheritdoc />
    public AdministracaoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AdministracaoDbContext>()
            .UseSqlServer("Server=localhost;Database=TensorrootGov;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new AdministracaoDbContext(options, new TenantDesignTime());
    }

    private sealed class TenantDesignTime : ITenantContext
    {
        public Guid TenantId => Guid.Empty;

        public bool HasTenant => false;
    }
}
