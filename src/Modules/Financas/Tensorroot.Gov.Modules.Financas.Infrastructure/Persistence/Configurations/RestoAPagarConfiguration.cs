using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.RestosAPagar;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="RestoAPagar"/>.</summary>
public sealed class RestoAPagarConfiguration : IEntityTypeConfiguration<RestoAPagar>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RestoAPagar> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RestosAPagar");
        builder.HasKey(resto => resto.Id);
        builder.Property(resto => resto.Id)
            .HasConversion(id => id.Value, value => new RestoAPagarId(value))
            .ValueGeneratedNever();

        builder.Property(resto => resto.EmpenhoId)
            .HasConversion(id => id.Value, value => new EmpenhoId(value));

        builder.Property(resto => resto.Classificacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(resto => resto.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.Property(resto => resto.ValorInscrito)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(resto => resto.ValorLiquidado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(resto => resto.ValorPago)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(resto => resto.ValorCancelado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        // Derivado não persistido.
        builder.Ignore(resto => resto.SaldoAPagar);

        builder.HasIndex(resto => new { resto.TenantId, resto.ExercicioInscricao });
        builder.HasIndex(resto => resto.EmpenhoId);
    }
}
