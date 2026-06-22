using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Configurations;

using DiarioClasseAggregate = Domain.DiarioClasse.DiarioClasse;

/// <summary>Mapeamento EF Core do agregado <see cref="DiarioClasseAggregate"/> e de suas entidades filhas.</summary>
public sealed class DiarioClasseConfiguration : IEntityTypeConfiguration<DiarioClasseAggregate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DiarioClasseAggregate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("DiariosClasse");
        builder.HasKey(diario => diario.Id);
        builder.Property(diario => diario.Id)
            .HasConversion(id => id.Value, value => new DiarioClasseId(value))
            .ValueGeneratedNever();

        builder.Property(diario => diario.MatriculaId)
            .HasConversion(id => id.Value, value => new MatriculaId(value));

        builder.Property(diario => diario.CargaHorariaTotal);
        builder.Property(diario => diario.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(diario => diario.Resultado).HasConversion<string?>().HasMaxLength(30);

        // Propriedades calculadas (sem coluna).
        builder.Ignore(diario => diario.PercentualFrequencia);
        builder.Ignore(diario => diario.DiasLetivosRegistrados);

        builder.OwnsMany(diario => diario.Frequencias, MapearFrequencias);
        builder.OwnsMany(diario => diario.Notas, MapearNotas);
        builder.OwnsMany(diario => diario.Aulas, MapearAulas);

        builder.Navigation(diario => diario.Frequencias).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(diario => diario.Notas).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(diario => diario.Aulas).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Vinculo 1-1 com a matricula (I-7): uma matricula admite um unico diario por tenant.
        builder.HasIndex(diario => new { diario.TenantId, diario.MatriculaId }).IsUnique();
    }

    private static void MapearFrequencias(OwnedNavigationBuilder<DiarioClasseAggregate, RegistroFrequencia> frequencias)
    {
        frequencias.ToTable("DiariosClasseFrequencias");
        frequencias.WithOwner().HasForeignKey("DiarioClasseId");
        frequencias.HasKey(frequencia => frequencia.Id);
        frequencias.Property(frequencia => frequencia.Id)
            .HasConversion(id => id.Value, value => new RegistroFrequenciaId(value))
            .ValueGeneratedNever();
        frequencias.Property(frequencia => frequencia.Data);
        frequencias.Property(frequencia => frequencia.Presente);
        frequencias.Property(frequencia => frequencia.CargaHorariaAula);
    }

    private static void MapearNotas(OwnedNavigationBuilder<DiarioClasseAggregate, RegistroNota> notas)
    {
        notas.ToTable("DiariosClasseNotas");
        notas.WithOwner().HasForeignKey("DiarioClasseId");
        notas.HasKey(nota => nota.Id);
        notas.Property(nota => nota.Id)
            .HasConversion(id => id.Value, value => new RegistroNotaId(value))
            .ValueGeneratedNever();
        notas.Property(nota => nota.Componente)
            .HasConversion(id => id.Value, value => new ComponenteCurricularId(value));
        notas.Property(nota => nota.Periodo).HasMaxLength(20);
        notas.Property(nota => nota.Valor).HasColumnType("decimal(5,2)");
    }

    private static void MapearAulas(OwnedNavigationBuilder<DiarioClasseAggregate, RegistroAula> aulas)
    {
        aulas.ToTable("DiariosClasseAulas");
        aulas.WithOwner().HasForeignKey("DiarioClasseId");
        aulas.HasKey(aula => aula.Id);
        aulas.Property(aula => aula.Id)
            .HasConversion(id => id.Value, value => new RegistroAulaId(value))
            .ValueGeneratedNever();
        aulas.Property(aula => aula.Data);
        aulas.Property(aula => aula.Conteudo).HasMaxLength(1000);
        aulas.Property(aula => aula.DiaLetivo);
    }
}
