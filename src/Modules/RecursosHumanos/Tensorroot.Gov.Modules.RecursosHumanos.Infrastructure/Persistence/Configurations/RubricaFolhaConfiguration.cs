using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="RubricaFolha"/> (catalogo parametrizavel de verbas).</summary>
public sealed class RubricaFolhaConfiguration : IEntityTypeConfiguration<RubricaFolha>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RubricaFolha> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Rubricas");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new RubricaFolhaId(value))
            .ValueGeneratedNever();

        builder.Property(r => r.Codigo)
            .HasConversion(codigo => codigo.Codigo, valor => Rubrica.De(valor))
            .HasMaxLength(RubricaFolha.CodigoComprimentoMaximo);

        builder.Property(r => r.Descricao).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Natureza).HasConversion<string>().HasMaxLength(20);

        builder.Property(r => r.VigenciaInicio)
            .HasConversion(
                competencia => (competencia.Ano * 100) + competencia.Mes,
                valor => Competencia.De(valor / 100, valor % 100));

        builder.Property(r => r.ValorFixo).HasColumnType("decimal(18,2)");
        builder.Property(r => r.Percentual).HasColumnType("decimal(9,6)");

        builder.Ignore(r => r.EhDesconto);
        builder.Ignore(r => r.EhProvento);

        // Codigo unico por tenant (catalogo de rubricas vigentes).
        builder.HasIndex(r => new { r.TenantId, r.Codigo }).IsUnique();
    }
}
