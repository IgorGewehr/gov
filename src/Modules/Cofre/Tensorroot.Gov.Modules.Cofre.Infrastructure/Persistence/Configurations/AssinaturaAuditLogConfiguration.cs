using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Cofre.Domain;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core da trilha imutavel de USO da assinatura (A1-DESIGN §6). So contem metadados
/// nao sigilosos (quem/quando/cert/destino/hash/IP/resultado); NUNCA material sensivel.
/// </summary>
public sealed class AssinaturaAuditLogConfiguration : IEntityTypeConfiguration<AssinaturaAuditLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AssinaturaAuditLog> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AssinaturaAuditLog");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).ValueGeneratedNever();
        builder.Property(log => log.TenantId).IsRequired();
        builder.Property(log => log.UserId).HasMaxLength(128);
        builder.Property(log => log.TimestampUtc).IsRequired();
        builder.Property(log => log.Thumbprint).HasMaxLength(128);
        builder.Property(log => log.Titular).HasMaxLength(CertificadoA1Cofre.ComprimentoMaximoTitular);
        builder.Property(log => log.Destino).HasMaxLength(32).IsRequired();
        builder.Property(log => log.HashArtefatoSha256).HasMaxLength(64);
        builder.Property(log => log.CorrelationId).HasMaxLength(128);
        builder.Property(log => log.IpAddress).HasMaxLength(64);
        builder.Property(log => log.Sucesso).IsRequired();
        builder.Property(log => log.Motivo).HasMaxLength(512);

        builder.HasIndex(log => new { log.TenantId, log.TimestampUtc });
    }
}
