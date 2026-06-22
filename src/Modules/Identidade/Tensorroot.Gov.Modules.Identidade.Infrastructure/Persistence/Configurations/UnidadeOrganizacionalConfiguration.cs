using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Identidade.Domain.Unidades;

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core do agregado <see cref="UnidadeOrganizacional"/> (arvore de UOs do tenant,
/// auto-relacionada por <see cref="UnidadeOrganizacional.UnidadePaiId"/>). Codigo unico por tenant
/// (usado em trilha e remessas). Nunca e deletada — apenas desativada (coluna <c>Ativa</c>).
/// </summary>
public sealed class UnidadeOrganizacionalConfiguration : IEntityTypeConfiguration<UnidadeOrganizacional>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UnidadeOrganizacional> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("UnidadesOrganizacionais");
        builder.HasKey(unidade => unidade.Id);
        builder.Property(unidade => unidade.Id)
            .HasConversion(id => id.Value, value => new UnidadeOrganizacionalId(value))
            .ValueGeneratedNever();

        builder.Property(unidade => unidade.TenantId).IsRequired();

        builder.Property(unidade => unidade.Codigo)
            .HasMaxLength(UnidadeOrganizacional.ComprimentoMaximoCodigo)
            .IsRequired();

        builder.Property(unidade => unidade.Nome)
            .HasMaxLength(UnidadeOrganizacional.ComprimentoMaximoNome)
            .IsRequired();

        builder.Property(unidade => unidade.Tipo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(unidade => unidade.UnidadePaiId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (UnidadeOrganizacionalId?)null : new UnidadeOrganizacionalId(value.Value))
            .HasColumnName("UnidadePaiId");

        builder.Property(unidade => unidade.Ativa).IsRequired();

        // Codigo unico POR TENANT (estavel — base de trilha/remessas).
        builder.HasIndex(unidade => new { unidade.TenantId, unidade.Codigo }).IsUnique();
        builder.HasIndex(unidade => new { unidade.TenantId, unidade.UnidadePaiId });
    }
}
