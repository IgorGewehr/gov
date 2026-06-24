using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="ItemCatalogo"/> (CATMAT/CATSER).</summary>
public sealed class CatalogoConfiguration : IEntityTypeConfiguration<ItemCatalogo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ItemCatalogo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CatalogoItens");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ItemCatalogoId(value))
            .ValueGeneratedNever();

        builder.Property(item => item.Codigo).HasMaxLength(40);
        builder.Property(item => item.Natureza).HasConversion<string>().HasMaxLength(20);
        builder.Property(item => item.Descricao).HasMaxLength(500);
        builder.Property(item => item.UnidadeFornecimento).HasMaxLength(20);
        builder.Property(item => item.Classe).HasMaxLength(120);
        builder.Property(item => item.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(item => new { item.TenantId, item.Codigo }).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.Natureza, item.Situacao });
    }
}
