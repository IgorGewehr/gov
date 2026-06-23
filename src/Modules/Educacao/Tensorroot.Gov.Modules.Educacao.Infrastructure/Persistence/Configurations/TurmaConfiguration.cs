using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Turma"/>.</summary>
public sealed class TurmaConfiguration : IEntityTypeConfiguration<Turma>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Turma> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Turmas");
        builder.HasKey(turma => turma.Id);
        builder.Property(turma => turma.Id)
            .HasConversion(id => id.Value, value => new TurmaId(value))
            .ValueGeneratedNever();

        builder.Property(turma => turma.EscolaId)
            .HasConversion(id => id.Value, value => new EscolaId(value));

        builder.Property(turma => turma.AnoLetivo);
        builder.Property(turma => turma.Etapa).HasConversion<string>().HasMaxLength(30);
        builder.Property(turma => turma.Serie).HasMaxLength(Turma.ComprimentoSerie);
        builder.Property(turma => turma.Turno).HasConversion<string>().HasMaxLength(20);
        builder.Property(turma => turma.Vagas);
        builder.Property(turma => turma.Matriculados);
        builder.Property(turma => turma.Situacao).HasConversion<string>().HasMaxLength(20);

        // Propriedades calculadas (sem coluna).
        builder.Ignore(turma => turma.VagasDisponiveis);
        builder.Ignore(turma => turma.PossuiVaga);

        // Unicidade da combinacao (escola, ano letivo, serie, turno) por tenant (I-T4).
        builder.HasIndex(turma => new { turma.TenantId, turma.EscolaId, turma.AnoLetivo, turma.Serie, turma.Turno }).IsUnique();
        builder.HasIndex(turma => new { turma.TenantId, turma.EscolaId, turma.AnoLetivo });
    }
}
