using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tensorroot.Gov.Platform.Tenancy;

namespace Tensorroot.Gov.Platform.Persistence;

/// <summary>
/// DbContext da PLATAFORMA (schema "plataforma"): catálogo de tenants e suas licenças de módulo.
/// Não é isolado por tenant — é o registro central de quem assina o quê.
/// </summary>
public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    /// <summary>Tenants (entes assinantes).</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Licenças de módulo por tenant.</summary>
    public DbSet<TenantModule> TenantModules => Set<TenantModule>();

    /// <summary>Índice central email → tenant para resolução do tenant no login (Identidade).</summary>
    public DbSet<UsuarioTenantIndex> UsuariosTenantIndex => Set<UsuarioTenantIndex>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("plataforma");

        modelBuilder.Entity<Tenant>(builder =>
        {
            builder.ToTable("Tenants");
            builder.HasKey(tenant => tenant.Id);
            builder.Property(tenant => tenant.Cnpj).HasMaxLength(14);
            builder.Property(tenant => tenant.Nome).HasMaxLength(200);
            builder.Property(tenant => tenant.Poder).HasConversion<string>().HasMaxLength(20);
            // SEC-1: a connection string é persistida CIFRADA (envelope AES-256-GCM + KEK embrulhada
            // + AAD do tenant), em Base64 — bem maior que o texto puro; folga generosa.
            builder.Property(tenant => tenant.ConnectionString).HasMaxLength(2000);
            builder.HasIndex(tenant => tenant.Cnpj).IsUnique();
        });

        modelBuilder.Entity<TenantModule>(builder =>
        {
            builder.ToTable("TenantModules");
            builder.HasKey(vinculo => new { vinculo.TenantId, vinculo.ModuleName });
            builder.Property(vinculo => vinculo.ModuleName).HasMaxLength(60);
        });

        modelBuilder.Entity<UsuarioTenantIndex>(builder =>
        {
            builder.ToTable("UsuariosTenantIndex");
            // O e-mail e a chave global: um endereco pertence a no maximo um tenant.
            builder.HasKey(indice => indice.Email);
            builder.Property(indice => indice.Email).HasMaxLength(254);
        });

        base.OnModelCreating(modelBuilder);
    }
}

/// <summary>Fábrica de design-time do <see cref="PlatformDbContext"/> (para <c>dotnet ef</c>).</summary>
public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    /// <inheritdoc />
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer("Server=localhost;Database=TensorrootGov;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new PlatformDbContext(options);
    }
}
