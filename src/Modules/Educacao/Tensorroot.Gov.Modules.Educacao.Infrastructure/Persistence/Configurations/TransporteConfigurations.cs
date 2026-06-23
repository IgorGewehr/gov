using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.Modules.Educacao.Domain.Transporte;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="RotaTransporte"/> e da entidade-filha <see cref="AlunoTransportado"/>.</summary>
public sealed class RotaTransporteConfiguration : IEntityTypeConfiguration<RotaTransporte>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RotaTransporte> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RotasTransporte");
        builder.HasKey(rota => rota.Id);
        builder.Property(rota => rota.Id)
            .HasConversion(id => id.Value, value => new RotaTransporteId(value))
            .ValueGeneratedNever();

        builder.Property(rota => rota.EscolaId)
            .HasConversion(id => id.Value, value => new EscolaId(value));

        builder.Property(rota => rota.Nome).HasMaxLength(RotaTransporte.ComprimentoNome);
        builder.Property(rota => rota.Turno).HasConversion<string>().HasMaxLength(20);
        builder.Property(rota => rota.Modalidade).HasConversion<string>().HasMaxLength(20);
        builder.Property(rota => rota.VeiculoId);
        builder.Property(rota => rota.Quilometragem).HasColumnType("decimal(10,2)");
        builder.Property(rota => rota.Situacao).HasConversion<string>().HasMaxLength(20);

        // Propriedade calculada (sem coluna).
        builder.Ignore(rota => rota.TotalAtivos);

        builder.OwnsMany(rota => rota.Alunos, MapearAlunos);
        builder.Navigation(rota => rota.Alunos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(rota => new { rota.TenantId, rota.EscolaId });
    }

    private static void MapearAlunos(OwnedNavigationBuilder<RotaTransporte, AlunoTransportado> alunos)
    {
        alunos.ToTable("RotasTransporteAlunos");
        alunos.WithOwner().HasForeignKey("RotaTransporteId");
        alunos.HasKey(aluno => aluno.Id);
        alunos.Property(aluno => aluno.Id)
            .HasConversion(id => id.Value, value => new AlunoTransportadoId(value))
            .ValueGeneratedNever();
        alunos.Property(aluno => aluno.AlunoId)
            .HasConversion(id => id.Value, value => new AlunoId(value));
        alunos.Property(aluno => aluno.MatriculaId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? null : new MatriculaId(value.Value));
        alunos.Property(aluno => aluno.PontoEmbarque).HasMaxLength(AlunoTransportado.ComprimentoPonto);
        alunos.Property(aluno => aluno.Ativo);
    }
}
