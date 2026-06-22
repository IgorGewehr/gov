using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Legislativo.Domain.DiarioOficial;

namespace Tensorroot.Gov.Modules.Legislativo.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="EdicaoDiario"/> e de suas materias.</summary>
public sealed class EdicaoDiarioConfiguration : IEntityTypeConfiguration<EdicaoDiario>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EdicaoDiario> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("DiarioEdicoes");
        builder.HasKey(edicao => edicao.Id);
        builder.Property(edicao => edicao.Id)
            .HasConversion(id => id.Value, value => new EdicaoDiarioId(value))
            .ValueGeneratedNever();

        builder.Property(edicao => edicao.Numero);
        builder.Property(edicao => edicao.Ano);
        builder.Property(edicao => edicao.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(edicao => edicao.DataPublicacao);
        builder.Property(edicao => edicao.HashConteudo).HasMaxLength(128);

        builder.Property(edicao => edicao.EdicaoOriginalId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new EdicaoDiarioId(value.Value) : null);

        builder.Ignore(edicao => edicao.Publicada);

        builder.OwnsMany(edicao => edicao.Materias, MapearMaterias);
        builder.Navigation(edicao => edicao.Materias).UsePropertyAccessMode(PropertyAccessMode.Field);

        // D-5: numero unico por (tenant, ano).
        builder.HasIndex(edicao => new { edicao.TenantId, edicao.Ano, edicao.Numero }).IsUnique();
        builder.HasIndex(edicao => new { edicao.TenantId, edicao.Situacao });
    }

    private static void MapearMaterias(OwnedNavigationBuilder<EdicaoDiario, MateriaDiario> materias)
    {
        materias.ToTable("DiarioMaterias");
        materias.WithOwner().HasForeignKey("EdicaoOwnerId");
        materias.HasKey(materia => materia.Id);
        materias.Property(materia => materia.Id)
            .HasConversion(id => id.Value, value => new MateriaDiarioId(value))
            .ValueGeneratedNever();
        materias.Property(materia => materia.Tipo).HasConversion<string>().HasMaxLength(20);
        materias.Property(materia => materia.Titulo).HasMaxLength(MateriaDiario.TituloMaximo);
        materias.Property(materia => materia.Conteudo);
        materias.Property(materia => materia.ReferenciaId);
        materias.Property(materia => materia.Ordem);
    }
}
