using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Saude.Domain.Profissionais;
using Tensorroot.Gov.SharedKernel.ValueObjects;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Profissional"/> e do vinculo CNES.</summary>
public sealed class ProfissionalConfiguration : IEntityTypeConfiguration<Profissional>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Profissional> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Profissionais");
        builder.HasKey(profissional => profissional.Id);
        builder.Property(profissional => profissional.Id)
            .HasConversion(id => id.Value, value => new ProfissionalId(value))
            .ValueGeneratedNever();

        builder.Property(profissional => profissional.Cpf)
            .HasConversion(cpf => cpf.Digitos, digitos => Cpf.Create(digitos))
            .HasMaxLength(11);

        builder.Property(profissional => profissional.Nome)
            .IsRequired()
            .HasMaxLength(Profissional.ComprimentoNome);

        builder.Property(profissional => profissional.Cns).HasMaxLength(Profissional.ComprimentoCns);

        builder.Property(profissional => profissional.Situacao).HasConversion<string>().HasMaxLength(20);

        // Registro de conselho (VO struct OPCIONAL): EF Core 8 nao suporta complex property nullable,
        // entao persiste-se como coluna unica compacta "Tipo|Uf|Numero" (nula quando ausente) via
        // value converter, preservando o VO no dominio sem expor colunas-sombra.
        builder.Property(profissional => profissional.Registro)
            .HasConversion(new RegistroConselhoConverter())
            .HasColumnName("Registro")
            .HasMaxLength(60);

        builder.OwnsMany(profissional => profissional.Vinculos, MapearVinculos);
        builder.Navigation(profissional => profissional.Vinculos).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Unicidade do CPF por tenant: um profissional por CPF no ente publico.
        builder.HasIndex(profissional => new { profissional.TenantId, profissional.Cpf }).IsUnique();
        builder.HasIndex(profissional => new { profissional.TenantId, profissional.Nome });
    }

    private static void MapearVinculos(OwnedNavigationBuilder<Profissional, VinculoCnes> vinculos)
    {
        vinculos.ToTable("ProfissionaisVinculos");
        vinculos.WithOwner().HasForeignKey("ProfissionalId");
        vinculos.HasKey(vinculo => vinculo.Id);
        vinculos.Property(vinculo => vinculo.Id)
            .HasConversion(id => id.Value, value => new VinculoCnesId(value))
            .ValueGeneratedNever();
        vinculos.Property(vinculo => vinculo.EstabelecimentoId)
            .HasConversion(id => id.Value, value => new EstabelecimentoId(value));
        vinculos.Property(vinculo => vinculo.Cbo)
            .HasConversion(cbo => cbo.Valor, valor => new Cbo(valor))
            .HasMaxLength(Cbo.Comprimento);
        vinculos.Property(vinculo => vinculo.DataInicio);
        vinculos.Property(vinculo => vinculo.DataFim);
        vinculos.HasIndex("ProfissionalId", "EstabelecimentoId");
    }
}
