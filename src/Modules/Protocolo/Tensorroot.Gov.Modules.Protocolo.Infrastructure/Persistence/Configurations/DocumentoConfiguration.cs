using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Documento"/>.</summary>
public sealed class DocumentoConfiguration : IEntityTypeConfiguration<Documento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Documento> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Documentos");
        builder.HasKey(documento => documento.Id);
        builder.Property(documento => documento.Id)
            .HasConversion(id => id.Value, value => new DocumentoId(value))
            .ValueGeneratedNever();

        // Hash SHA-256 (ValueObject) -> coluna unica de 64 hexadigitos.
        builder.Property(documento => documento.Hash)
            .HasConversion(hash => hash.Valor, valor => Hash.De(valor))
            .HasMaxLength(Hash.ComprimentoSha256)
            .IsRequired();

        builder.Property(documento => documento.Criticidade).HasConversion<string>().HasMaxLength(20);
        builder.Property(documento => documento.NivelAcesso).HasConversion<string>().HasMaxLength(20);
        builder.Property(documento => documento.TipoAssinatura).HasConversion<string>().HasMaxLength(30);
        builder.Property(documento => documento.MotivoSemEfeito).HasMaxLength(500);
        builder.Property(documento => documento.Situacao).HasConversion<string>().HasMaxLength(20);

        // Carimbo de tempo (ValueObject opcional) embutido na linha do documento.
        builder.OwnsOne(documento => documento.CarimboTempo, MapearCarimbo);

        builder.HasIndex(documento => new { documento.TenantId, documento.ProcessoId });
    }

    private static void MapearCarimbo(OwnedNavigationBuilder<Documento, CarimboDeTempo> carimbo)
    {
        carimbo.Property(item => item.InstanteUtc).HasColumnName("CarimboInstanteUtc");
        carimbo.Property(item => item.Autoridade)
            .HasColumnName("CarimboAutoridade")
            .HasMaxLength(CarimboDeTempo.ComprimentoMaximoAutoridade);
    }
}
