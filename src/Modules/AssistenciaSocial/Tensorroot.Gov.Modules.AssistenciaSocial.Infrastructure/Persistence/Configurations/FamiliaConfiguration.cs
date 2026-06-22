using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Familia"/> e de suas entidades filhas.</summary>
public sealed class FamiliaConfiguration : IEntityTypeConfiguration<Familia>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Familia> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Familias");
        builder.HasKey(familia => familia.Id);
        builder.Property(familia => familia.Id)
            .HasConversion(id => id.Value, value => new FamiliaId(value))
            .ValueGeneratedNever();

        builder.Property(familia => familia.Nis)
            .HasConversion(nis => nis.Digitos, valor => Nis.Create(valor))
            .HasMaxLength(11);

        builder.Property(familia => familia.CpfResponsavel)
            .HasConversion(cpf => cpf.Digitos, valor => Cpf.Create(valor))
            .HasMaxLength(11);

        builder.Property(familia => familia.UnidadeAtendimentoId);
        builder.Property(familia => familia.Territorio).HasMaxLength(120);
        builder.Property(familia => familia.DataReferenciamento);
        builder.Property(familia => familia.DataUltimaAtualizacaoCadastral);
        builder.Property(familia => familia.Situacao).HasConversion<string>().HasMaxLength(30);

        builder.Property(familia => familia.RendaPerCapita)
            .HasConversion(renda => renda.Valor, valor => RendaPerCapita.Calcular(valor, 1))
            .HasColumnType("decimal(18,2)");

        builder.OwnsOne(familia => familia.Endereco, MapearEndereco);
        builder.Navigation(familia => familia.Endereco).IsRequired();

        builder.OwnsMany(familia => familia.Membros, MapearMembros);
        builder.Navigation(familia => familia.Membros).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(familia => new { familia.TenantId, familia.Nis }).IsUnique();
        builder.HasIndex(familia => new { familia.TenantId, familia.Territorio });
    }

    private static void MapearEndereco(OwnedNavigationBuilder<Familia, EnderecoTerritorializado> endereco)
    {
        endereco.Property(item => item.Logradouro).HasMaxLength(200);
        endereco.Property(item => item.Municipio).HasMaxLength(120);
        endereco.Property(item => item.Cep).HasMaxLength(8);
        endereco.Property(item => item.Territorio).HasMaxLength(120);
    }

    private static void MapearMembros(OwnedNavigationBuilder<Familia, MembroFamiliar> membros)
    {
        membros.ToTable("FamiliasMembros");
        membros.WithOwner().HasForeignKey("FamiliaId");
        membros.HasKey(membro => membro.Id);
        membros.Property(membro => membro.Id)
            .HasConversion(id => id.Value, value => new MembroFamiliarId(value))
            .ValueGeneratedNever();

        membros.Property(membro => membro.Cpf)
            .HasConversion(cpf => cpf.Digitos, valor => Cpf.Create(valor))
            .HasMaxLength(11);
        membros.Property(membro => membro.Parentesco).HasConversion<string>().HasMaxLength(30);
        membros.Property(membro => membro.DataNascimento);
        membros.Property(membro => membro.RendaIndividual)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        membros.Property(membro => membro.EhPcd);
    }
}
