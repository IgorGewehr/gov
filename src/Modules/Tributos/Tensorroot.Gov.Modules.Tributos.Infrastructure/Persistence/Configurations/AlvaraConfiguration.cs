using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Alvaras;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Alvara"/> (ato de polícia).</summary>
public sealed class AlvaraConfiguration : IEntityTypeConfiguration<Alvara>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Alvara> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Alvaras");
        builder.HasKey(alvara => alvara.Id);
        builder.Property(alvara => alvara.Id)
            .HasConversion(id => id.Value, value => new AlvaraId(value))
            .ValueGeneratedNever();

        builder.Property(alvara => alvara.ContribuinteId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));
        builder.Property(alvara => alvara.ImovelId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (ImovelId?)null : new ImovelId(value.Value));

        builder.Property(alvara => alvara.Especie).HasConversion<string>().HasMaxLength(40);
        builder.Property(alvara => alvara.NomeEstabelecimento).HasMaxLength(200).IsRequired();
        builder.Property(alvara => alvara.AtividadeCnae).HasMaxLength(40).IsRequired();
        builder.Property(alvara => alvara.InicioVigencia);
        builder.Property(alvara => alvara.FimVigencia);
        builder.Property(alvara => alvara.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(alvara => new { alvara.TenantId, alvara.ContribuinteId });
    }
}
