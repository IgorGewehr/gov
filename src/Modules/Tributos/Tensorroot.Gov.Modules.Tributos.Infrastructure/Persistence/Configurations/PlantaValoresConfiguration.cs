using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="PlantaValores"/> (PGV) e suas entidades-filhas.</summary>
public sealed class PlantaValoresConfiguration : IEntityTypeConfiguration<PlantaValores>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlantaValores> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PlantasValores");
        builder.HasKey(planta => planta.Id);
        builder.Property(planta => planta.Id)
            .HasConversion(id => id.Value, value => new PlantaValoresId(value))
            .ValueGeneratedNever();

        builder.Property(planta => planta.Exercicio);
        builder.Property(planta => planta.FundamentoLegal).HasMaxLength(300).IsRequired();
        builder.Property(planta => planta.Vigente);

        builder.HasMany(planta => planta.Zonas).WithOne().HasForeignKey(z => z.PlantaValoresId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(planta => planta.Fatores).WithOne().HasForeignKey(f => f.PlantaValoresId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(planta => planta.Zonas).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(planta => planta.Fatores).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(planta => new { planta.TenantId, planta.Exercicio });
    }
}

/// <summary>Mapeamento EF Core da entidade <see cref="ValorZona"/> (VUT/VUC por zona).</summary>
public sealed class ValorZonaConfiguration : IEntityTypeConfiguration<ValorZona>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ValorZona> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PgvValoresZona");
        builder.HasKey(zona => zona.Id);
        builder.Property(zona => zona.Id)
            .HasConversion(id => id.Value, value => new ValorZonaId(value))
            .ValueGeneratedNever();

        builder.Property(zona => zona.PlantaValoresId)
            .HasConversion(id => id.Value, value => new PlantaValoresId(value));

        builder.Property(zona => zona.ZonaFiscal).HasMaxLength(60).IsRequired();
        builder.Property(zona => zona.ValorM2Terreno).HasColumnType("decimal(18,2)");
        builder.Property(zona => zona.ValorM2Construcao).HasColumnType("decimal(18,2)");

        builder.HasIndex(zona => new { zona.PlantaValoresId, zona.ZonaFiscal }).IsUnique();
    }
}

/// <summary>Mapeamento EF Core da entidade <see cref="FatorPgv"/> (fatores de correção).</summary>
public sealed class FatorPgvConfiguration : IEntityTypeConfiguration<FatorPgv>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FatorPgv> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PgvFatores");
        builder.HasKey(fator => fator.Id);
        builder.Property(fator => fator.Id)
            .HasConversion(id => id.Value, value => new FatorPgvId(value))
            .ValueGeneratedNever();

        builder.Property(fator => fator.PlantaValoresId)
            .HasConversion(id => id.Value, value => new PlantaValoresId(value));

        builder.Property(fator => fator.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(fator => fator.Chave).HasMaxLength(60).IsRequired();
        builder.Property(fator => fator.Multiplicador).HasColumnType("decimal(9,4)");

        builder.HasIndex(fator => new { fator.PlantaValoresId, fator.Tipo, fator.Chave }).IsUnique();
    }
}
