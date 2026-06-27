using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Condutor"/> (motorista habilitado da frota; LGPD).</summary>
public sealed class CondutorConfiguration : IEntityTypeConfiguration<Condutor>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Condutor> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Condutores");
        builder.HasKey(condutor => condutor.Id);
        builder.Property(condutor => condutor.Id)
            .HasConversion(id => id.Value, value => new CondutorId(value))
            .ValueGeneratedNever();

        builder.Property(condutor => condutor.Nome).HasMaxLength(200);
        builder.Property(condutor => condutor.Cpf).HasMaxLength(11);
        builder.Property(condutor => condutor.NumeroCnh).HasMaxLength(20);
        builder.Property(condutor => condutor.Categorias).HasMaxLength(10);
        builder.Property(condutor => condutor.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(condutor => condutor.MotivoSuspensao).HasMaxLength(200);

        builder.HasIndex(condutor => new { condutor.TenantId, condutor.NumeroCnh }).IsUnique();
        builder.HasIndex("TenantId", "ValidadeCnh");
    }
}
