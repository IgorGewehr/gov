using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;
using Tensorroot.Gov.Modules.Administracao.Domain.Pca;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="PlanoContratacoes"/> (PCA) e de seus itens.</summary>
public sealed class PcaConfiguration : IEntityTypeConfiguration<PlanoContratacoes>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlanoContratacoes> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PlanosContratacoes");
        builder.HasKey(plano => plano.Id);
        builder.Property(plano => plano.Id)
            .HasConversion(id => id.Value, value => new PlanoContratacoesId(value))
            .ValueGeneratedNever();

        builder.Property(plano => plano.Exercicio);
        builder.Property(plano => plano.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(plano => plano.NumeroPncp).HasMaxLength(60);
        builder.Ignore(plano => plano.ValorTotalEstimado);

        builder.OwnsMany(plano => plano.Itens, MapearItens);
        builder.Navigation(plano => plano.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(plano => new { plano.TenantId, plano.Exercicio }).IsUnique();
    }

    private static void MapearItens(OwnedNavigationBuilder<PlanoContratacoes, ItemPca> itens)
    {
        itens.ToTable("PlanosContratacoesItens");
        itens.WithOwner().HasForeignKey("PlanoContratacoesId");
        itens.HasKey(item => item.Id);
        itens.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ItemPcaId(value))
            .ValueGeneratedNever();
        itens.Property(item => item.ItemCatalogoId)
            .HasConversion(id => id.Value, value => new ItemCatalogoId(value));
        itens.Property(item => item.Quantidade).HasColumnType("decimal(18,4)");
        itens.Property(item => item.ValorEstimado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        itens.Property(item => item.TrimestreDesejado);
        itens.Property(item => item.Justificativa).HasMaxLength(2000);
    }
}
