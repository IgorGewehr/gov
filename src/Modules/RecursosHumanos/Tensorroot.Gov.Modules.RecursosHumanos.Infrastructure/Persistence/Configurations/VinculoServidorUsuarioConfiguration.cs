using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do vinculo <see cref="VinculoServidorUsuario"/> (usuario&#8596;servidor).</summary>
public sealed class VinculoServidorUsuarioConfiguration : IEntityTypeConfiguration<VinculoServidorUsuario>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<VinculoServidorUsuario> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("VinculosServidorUsuario");
        builder.HasKey(vinculo => vinculo.Id);
        builder.Property(vinculo => vinculo.Id)
            .HasConversion(id => id.Value, value => new VinculoServidorUsuarioId(value))
            .ValueGeneratedNever();

        builder.Property(vinculo => vinculo.UsuarioId);

        builder.Property(vinculo => vinculo.ServidorId)
            .HasConversion(id => id.Value, value => new ServidorId(value));

        // Unicidade por tenant: um usuario para no maximo um servidor; um servidor para no maximo um
        // usuario. Ambos os indices incluem o TenantId — o vinculo e estritamente intra-tenant.
        builder.HasIndex(vinculo => new { vinculo.TenantId, vinculo.UsuarioId }).IsUnique();
        builder.HasIndex(vinculo => new { vinculo.TenantId, vinculo.ServidorId }).IsUnique();
    }
}
