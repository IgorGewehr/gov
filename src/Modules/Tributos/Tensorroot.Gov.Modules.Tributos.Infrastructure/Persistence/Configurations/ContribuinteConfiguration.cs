using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Contribuinte"/>.</summary>
public sealed class ContribuinteConfiguration : IEntityTypeConfiguration<Contribuinte>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Contribuinte> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Contribuintes");
        builder.HasKey(contribuinte => contribuinte.Id);
        builder.Property(contribuinte => contribuinte.Id)
            .HasConversion(id => id.Value, value => new ContribuinteId(value))
            .ValueGeneratedNever();

        builder.Property(contribuinte => contribuinte.TipoPessoa).HasConversion<string>().HasMaxLength(20);
        builder.Property(contribuinte => contribuinte.Documento).HasMaxLength(14);
        builder.Property(contribuinte => contribuinte.Nome).HasMaxLength(200);
        builder.Property(contribuinte => contribuinte.InscricaoMunicipal).HasMaxLength(30);

        builder.HasIndex(contribuinte => new { contribuinte.TenantId, contribuinte.Documento });
    }
}
