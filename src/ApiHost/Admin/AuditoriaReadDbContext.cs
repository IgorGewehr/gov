using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Auditing;

namespace Tensorroot.Gov.ApiHost.Admin;

/// <summary>
/// Contexto SOMENTE-LEITURA da trilha de auditoria do tenant (banco DEDICADO), usado pelo
/// visualizador administrativo. Mapeia apenas <see cref="AuditTrail"/>.
/// <para>
/// Em SQLite (dev), os módulos compartilham a tabela <c>AuditTrail</c> no arquivo do tenant, então
/// uma única consulta enxerga toda a trilha. Em SqlServer (multi-schema por módulo), a agregação
/// entre schemas é uma evolução futura (visualizador por módulo ou tabela de auditoria unificada).
/// </para>
/// </summary>
public sealed class AuditoriaReadDbContext(DbContextOptions<AuditoriaReadDbContext> options) : DbContext(options)
{
    /// <summary>Trilha de auditoria (somente leitura).</summary>
    public DbSet<AuditTrail> Trilha => Set<AuditTrail>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        var entidade = modelBuilder.Entity<AuditTrail>();
        entidade.ToTable("AuditTrail");
        entidade.HasKey(item => item.Id);
    }
}
