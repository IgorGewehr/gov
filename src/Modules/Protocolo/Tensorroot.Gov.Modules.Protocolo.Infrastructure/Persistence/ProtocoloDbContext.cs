using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure;
using Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Protocolo (schema isolado "protocolo"), herdando Outbox, Audit Trail
/// e o Global Query Filter por tenant de <see cref="ModuleDbContext"/>. Implementa <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class ProtocoloDbContext(DbContextOptions<ProtocoloDbContext> options, ITenantContext tenantContext, ScopeDbContextHolder? holder = null)
    : ModuleDbContext(options, tenantContext, holder), IUnitOfWork
{
    /// <inheritdoc />
    public override string Schema => "protocolo";

    /// <summary>Processos administrativos eletronicos (PAE — raiz de agregado).</summary>
    public DbSet<Processo> Processos => Set<Processo>();

    /// <summary>Documentos (pecas processuais — raiz de agregado).</summary>
    public DbSet<Documento> Documentos => Set<Documento>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProtocoloDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
