using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Servidor"/> e de suas entidades filhas.</summary>
public sealed class ServidorConfiguration : IEntityTypeConfiguration<Servidor>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Servidor> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Servidores");
        builder.HasKey(servidor => servidor.Id);
        builder.Property(servidor => servidor.Id)
            .HasConversion(id => id.Value, value => new ServidorId(value))
            .ValueGeneratedNever();

        builder.Property(servidor => servidor.Cpf)
            .HasConversion(cpf => cpf.Digitos, digitos => Cpf.Create(digitos))
            .HasMaxLength(11);

        builder.Property(servidor => servidor.Matricula)
            .HasConversion(matricula => matricula.Valor, valor => Matricula.De(valor))
            .HasMaxLength(Matricula.ComprimentoMaximo);

        builder.Property(servidor => servidor.CargoId)
            .HasConversion(id => id.Value, value => new CargoId(value));

        builder.Property(servidor => servidor.Regime).HasConversion<string>().HasMaxLength(10);
        builder.Property(servidor => servidor.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.ComplexProperty(servidor => servidor.DadosPessoais, dados =>
        {
            dados.Property(d => d.Nome).HasColumnName("Nome").HasMaxLength(200);
            dados.Property(d => d.DataNascimento).HasColumnName("DataNascimento");
        });

        builder.OwnsMany(servidor => servidor.Dependentes, MapearDependentes);
        builder.Navigation(servidor => servidor.Dependentes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(servidor => new { servidor.TenantId, servidor.Matricula }).IsUnique();
    }

    private static void MapearDependentes(OwnedNavigationBuilder<Servidor, Dependente> dependentes)
    {
        dependentes.ToTable("ServidoresDependentes");
        dependentes.WithOwner().HasForeignKey("ServidorId");
        dependentes.HasKey(dependente => dependente.Id);
        dependentes.Property(dependente => dependente.Id)
            .HasConversion(id => id.Value, value => new DependenteId(value))
            .ValueGeneratedNever();
        dependentes.Property(dependente => dependente.Nome).HasMaxLength(200);
        dependentes.Property(dependente => dependente.Parentesco).HasMaxLength(50);
    }
}
