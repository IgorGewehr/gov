using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Vereadores;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Vereador"/> (cadastro de parlamentares).</summary>
public sealed class VereadorConfiguration : IEntityTypeConfiguration<Vereador>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Vereador> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Vereadores");
        builder.HasKey(vereador => vereador.Id);
        builder.Property(vereador => vereador.Id)
            .HasConversion(id => id.Value, value => new VereadorId(value))
            .ValueGeneratedNever();

        builder.Property(vereador => vereador.NomeCivil).HasMaxLength(Vereador.NomeMaximo).IsRequired();
        builder.Property(vereador => vereador.NomeParlamentar).HasMaxLength(Vereador.NomeMaximo).IsRequired();
        builder.Property(vereador => vereador.Partido).HasMaxLength(Vereador.PartidoMaximo).IsRequired();
        builder.Property(vereador => vereador.LegislaturaInicio);
        builder.Property(vereador => vereador.LegislaturaFim);
        builder.Property(vereador => vereador.CargoMesa).HasConversion<string>().HasMaxLength(20);
        builder.Property(vereador => vereador.Situacao).HasConversion<string>().HasMaxLength(20);

        // Propriedade calculada (sem coluna).
        builder.Ignore(vereador => vereador.Terminal);

        builder.HasIndex(vereador => new { vereador.TenantId, vereador.NomeParlamentar });
    }
}
