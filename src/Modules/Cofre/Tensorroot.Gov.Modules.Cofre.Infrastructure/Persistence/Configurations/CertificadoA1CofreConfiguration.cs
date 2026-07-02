using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Cofre.Domain;

namespace Tensorroot.Gov.Modules.Cofre.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do agregado <see cref="CertificadoA1Cofre"/>. As colunas de material (PfxCipher,
/// SenhaCipher, DekWrapped, nonces e tags) guardam APENAS bytes CIFRADOS (envelope AES-256-GCM); o
/// .pfx e a senha em claro NUNCA tocam o banco (A1-DESIGN §2). Um indice unico filtrado garante UM
/// unico certificado Ativo por tenant.
/// </summary>
public sealed class CertificadoA1CofreConfiguration : IEntityTypeConfiguration<CertificadoA1Cofre>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CertificadoA1Cofre> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CertificadosA1");
        builder.HasKey(certificado => certificado.Id);
        builder.Property(certificado => certificado.Id)
            .HasConversion(id => id.Value, value => new CertificadoA1CofreId(value))
            .ValueGeneratedNever();

        builder.Property(certificado => certificado.TenantId).IsRequired();
        builder.Property(certificado => certificado.Titular).HasMaxLength(CertificadoA1Cofre.ComprimentoMaximoTitular).IsRequired();
        builder.Property(certificado => certificado.CnpjTitular).HasMaxLength(14).IsRequired();
        builder.Property(certificado => certificado.Thumbprint).HasMaxLength(128).IsRequired();
        builder.Property(certificado => certificado.NotBeforeUtc).IsRequired();
        builder.Property(certificado => certificado.NotAfterUtc).IsRequired();
        builder.Property(certificado => certificado.Serie).HasMaxLength(8).IsRequired();

        // Material CIFRADO (envelope) — bytes opacos; nunca em claro.
        builder.Property(certificado => certificado.PfxCipher).IsRequired();
        builder.Property(certificado => certificado.PfxNonce).HasMaxLength(MaterialCifrado.TamanhoNonce).IsRequired();
        builder.Property(certificado => certificado.PfxTag).HasMaxLength(MaterialCifrado.TamanhoTag).IsRequired();
        builder.Property(certificado => certificado.SenhaCipher).IsRequired();
        builder.Property(certificado => certificado.SenhaNonce).HasMaxLength(MaterialCifrado.TamanhoNonce).IsRequired();
        builder.Property(certificado => certificado.SenhaTag).HasMaxLength(MaterialCifrado.TamanhoTag).IsRequired();
        builder.Property(certificado => certificado.DekWrapped).IsRequired();
        builder.Property(certificado => certificado.KekKeyId).HasMaxLength(512).IsRequired();

        builder.Property(certificado => certificado.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(certificado => certificado.CertificadoAnteriorId);

        builder.HasIndex(certificado => new { certificado.TenantId, certificado.Thumbprint }).IsUnique();

        // S2: indice UNICO FILTRADO — garante NO BANCO a invariante "no maximo UM Ativo por tenant"
        // (barra race na rotacao concorrente), alem de indexar a busca do Ativo. Filtro [Status]='Ativo'
        // e portavel Sqlite/SqlServer (mesmo padrao do AuditTrail).
        builder.HasIndex(certificado => new { certificado.TenantId, certificado.Status })
            .IsUnique()
            .HasFilter("[Status] = 'Ativo'");
    }
}
