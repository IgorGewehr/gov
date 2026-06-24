using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Portarias;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Portaria"/>.</summary>
public sealed class PortariaConfiguration : IEntityTypeConfiguration<Portaria>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Portaria> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Portarias");
        builder.HasKey(portaria => portaria.Id);
        builder.Property(portaria => portaria.Id)
            .HasConversion(id => id.Value, value => new PortariaId(value))
            .ValueGeneratedNever();

        // Numeracao: escalares (Exercicio, Sequencial) — chave da unicidade sequencial; o VO Numero e'
        // calculado (reconstruido do par) e nao tem coluna propria.
        builder.Property(portaria => portaria.Exercicio);
        builder.Property(portaria => portaria.Sequencial);
        builder.Ignore(portaria => portaria.Numero);

        builder.Property(portaria => portaria.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(portaria => portaria.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(portaria => portaria.DataAto);
        builder.Property(portaria => portaria.Ementa).HasMaxLength(Portaria.ComprimentoMaximoEmenta);
        builder.Property(portaria => portaria.Texto).HasMaxLength(Portaria.ComprimentoMaximoTexto);
        builder.Property(portaria => portaria.MotivoRevogacao).HasMaxLength(500);

        builder.Property(portaria => portaria.ServidorId)
            .HasConversion(
                servidor => servidor!.Value.Value,
                valor => new ServidorId(valor));

        // Unicidade da numeracao sequencial por exercicio no tenant.
        builder.HasIndex(portaria => new { portaria.TenantId, portaria.Exercicio, portaria.Sequencial })
            .IsUnique();

        builder.HasIndex(portaria => new { portaria.TenantId, portaria.Tipo, portaria.Situacao });
        builder.HasIndex(portaria => new { portaria.TenantId, portaria.ServidorId });
    }
}
