using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;
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

    /// <summary>Trilha de auditoria imutável (hash-chain) das mutações do control-plane (R3).</summary>
    public DbSet<AuditTrail> AuditTrail => Set<AuditTrail>();

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

        // R3: trilha de auditoria do control-plane — MESMA forma da trilha dos módulos. Como as
        // entidades da plataforma não têm tenant, o interceptor as sela numa cadeia ÚNICA (Guid.Empty).
        // Índice único filtrado em (TenantId, Sequencia > 0) preserva o "último selo" e barra bifurcação.
        modelBuilder.Entity<AuditTrail>(builder =>
        {
            builder.ToTable("AuditTrail");
            builder.HasKey(trail => trail.Id);
            builder.Property(trail => trail.EntityName).HasMaxLength(256);
            builder.Property(trail => trail.Action).HasMaxLength(20);
            builder.Property(trail => trail.HashAnterior).HasMaxLength(64);
            builder.Property(trail => trail.HashAtual).HasMaxLength(64);
            builder.HasIndex(trail => new { trail.TenantId, trail.TimestampUtc });
            builder
                .HasIndex(trail => new { trail.TenantId, trail.Sequencia })
                .IsUnique()
                .HasFilter("[Sequencia] > 0");
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
