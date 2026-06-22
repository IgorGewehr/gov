using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Cofre.Domain;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Persistence;

/// <summary>
/// DbContext do modulo Cofre (schema isolado "cofre"), no banco DEDICADO do tenant. Herda Outbox,
/// Audit Trail e o Global Query Filter por tenant de <see cref="ModuleDbContext"/> e implementa
/// <see cref="IUnitOfWork"/>. E SEGURANCA CRITICA: o .pfx e a senha vivem aqui apenas CIFRADOS
/// (AES-256-GCM, envelope); a chave privada NUNCA toca o banco.
/// </summary>
public sealed class CofreDbContext(DbContextOptions<CofreDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "cofre";

    /// <summary>Certificados A1 custodiados (material cifrado) do tenant.</summary>
    public DbSet<CertificadoA1Cofre> Certificados => Set<CertificadoA1Cofre>();

    /// <summary>Trilha imutavel de auditoria de USO da assinatura (sucesso e falha) do tenant.</summary>
    public DbSet<AssinaturaAuditLog> AssinaturaAuditLogs => Set<AssinaturaAuditLog>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CofreDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
