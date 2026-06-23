using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Matricula"/> e de seu objeto de valor owned.</summary>
public sealed class MatriculaConfiguration : IEntityTypeConfiguration<Matricula>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Matricula> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Matriculas");
        builder.HasKey(matricula => matricula.Id);
        builder.Property(matricula => matricula.Id)
            .HasConversion(id => id.Value, value => new MatriculaId(value))
            .ValueGeneratedNever();

        builder.Property(matricula => matricula.AlunoId)
            .HasConversion(id => id.Value, value => new AlunoId(value));
        builder.Property(matricula => matricula.TurmaId)
            .HasConversion(id => id.Value, value => new TurmaId(value));
        builder.Property(matricula => matricula.EscolaId)
            .HasConversion(id => id.Value, value => new EscolaId(value));

        builder.Property(matricula => matricula.Situacao).HasConversion<string>().HasMaxLength(20);

        // Situacao do Aluno (2a etapa do Censo) — objeto de valor opcional owned.
        builder.OwnsOne(matricula => matricula.SituacaoDoAluno, MapearSituacaoDoAluno);

        builder.HasIndex(matricula => new { matricula.TenantId, matricula.AlunoId });
        builder.HasIndex(matricula => new { matricula.TenantId, matricula.TurmaId, matricula.DataReferencia });
    }

    private static void MapearSituacaoDoAluno(OwnedNavigationBuilder<Matricula, SituacaoDoAluno> situacao)
    {
        situacao.Property(item => item.Rendimento).HasConversion<string>().HasColumnName("SituacaoRendimento").HasMaxLength(20);
        situacao.Property(item => item.Movimento).HasConversion<string>().HasColumnName("SituacaoMovimento").HasMaxLength(20);
    }
}
