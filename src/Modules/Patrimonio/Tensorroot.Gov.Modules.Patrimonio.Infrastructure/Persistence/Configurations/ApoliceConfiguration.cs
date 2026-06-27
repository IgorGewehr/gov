using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Apolice"/> (apólice de seguro de veículo da frota).</summary>
public sealed class ApoliceConfiguration : IEntityTypeConfiguration<Apolice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Apolice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Apolices");
        builder.HasKey(apolice => apolice.Id);
        builder.Property(apolice => apolice.Id)
            .HasConversion(id => id.Value, value => new ApoliceId(value))
            .ValueGeneratedNever();

        builder.Property(apolice => apolice.VeiculoId)
            .HasConversion(id => id.Value, value => new VeiculoId(value));

        builder.Property(apolice => apolice.Categoria).HasConversion<string>().HasMaxLength(30);
        builder.Property(apolice => apolice.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(apolice => apolice.Seguradora).HasMaxLength(150);
        builder.Property(apolice => apolice.NumeroApolice).HasMaxLength(50);
        builder.Property(apolice => apolice.Cobertura).HasMaxLength(500);
        builder.Property(apolice => apolice.MotivoCancelamento).HasMaxLength(200);

        builder.Property(apolice => apolice.Premio)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(apolice => apolice.ImportanciaSegurada)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        // Propriedades calculadas (sem coluna).
        builder.Ignore(apolice => apolice.EhSeguroObrigatorio);

        builder.HasIndex("TenantId", "VeiculoId");
        builder.HasIndex(apolice => new { apolice.TenantId, apolice.FimVigencia });
    }
}
