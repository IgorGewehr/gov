using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Fornecedor"/> e de suas entidades filhas.</summary>
public sealed class FornecedorConfiguration : IEntityTypeConfiguration<Fornecedor>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Fornecedor> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Fornecedores");
        builder.HasKey(fornecedor => fornecedor.Id);
        builder.Property(fornecedor => fornecedor.Id)
            .HasConversion(id => id.Value, value => new FornecedorId(value))
            .ValueGeneratedNever();

        builder.Property(fornecedor => fornecedor.RazaoSocial).HasMaxLength(200);
        builder.Property(fornecedor => fornecedor.NivelCadastralSICAF).HasConversion<string>().HasMaxLength(40);
        builder.Property(fornecedor => fornecedor.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.Property(fornecedor => fornecedor.Cnpj)
            .HasConversion(cnpj => cnpj.Digitos, valor => Cnpj.Create(valor))
            .HasMaxLength(14);

        builder.OwnsMany(fornecedor => fornecedor.Sancoes, MapearSancoes);
        builder.Navigation(fornecedor => fornecedor.Sancoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Unicidade de CNPJ por tenant (I-4).
        builder.HasIndex(fornecedor => new { fornecedor.TenantId, fornecedor.Cnpj }).IsUnique();
    }

    private static void MapearSancoes(OwnedNavigationBuilder<Fornecedor, Sancao> sancoes)
    {
        sancoes.ToTable("FornecedoresSancoes");
        sancoes.WithOwner().HasForeignKey("FornecedorId");
        sancoes.HasKey(sancao => sancao.Id);
        sancoes.Property(sancao => sancao.Id)
            .HasConversion(id => id.Value, value => new SancaoId(value))
            .ValueGeneratedNever();
        sancoes.Property(sancao => sancao.Tipo).HasConversion<string>().HasMaxLength(20);
        sancoes.Property(sancao => sancao.ProcessoAdministrativo).HasMaxLength(60);
        sancoes.Property(sancao => sancao.Fundamentacao).HasMaxLength(2000);
        sancoes.Property(sancao => sancao.ValorMulta)
            .HasConversion(valor => valor!.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        sancoes.Ignore(sancao => sancao.EImpeditiva);
    }
}
