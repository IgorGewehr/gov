using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Beneficio"/>.</summary>
public sealed class BeneficioConfiguration : IEntityTypeConfiguration<Beneficio>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Beneficio> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Beneficios");
        builder.HasKey(beneficio => beneficio.Id);
        builder.Property(beneficio => beneficio.Id)
            .HasConversion(id => id.Value, value => new BeneficioId(value))
            .ValueGeneratedNever();

        builder.Property(beneficio => beneficio.FamiliaId);
        builder.Property(beneficio => beneficio.Tipo).HasConversion<string>().HasMaxLength(30);

        builder.Property(beneficio => beneficio.Competencia)
            .HasConversion(competencia => (competencia.Ano * 100) + competencia.Mes, valor => Competencia.De(valor / 100, valor % 100));

        builder.Property(beneficio => beneficio.Valor)
            .HasConversion(valor => valor!.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.Property(beneficio => beneficio.Situacao).HasConversion<string>().HasMaxLength(30);
        builder.Property(beneficio => beneficio.MotivoIndeferimento).HasMaxLength(500);
        builder.Property(beneficio => beneficio.DataDecisao);
        builder.Property(beneficio => beneficio.QuantidadeCesta);
        builder.Property(beneficio => beneficio.DataEntregaCesta);

        // EstaDecidido e propriedade calculada (sem coluna).
        builder.Ignore(beneficio => beneficio.EstaDecidido);

        builder.HasIndex(beneficio => new { beneficio.TenantId, beneficio.FamiliaId });
        builder.HasIndex(beneficio => new { beneficio.TenantId, beneficio.Situacao });
    }
}
