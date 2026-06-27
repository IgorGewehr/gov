using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="TabelaIrrfServicos"/> e das faixas (IN RFB 1.234/2012).</summary>
public sealed class TabelaIrrfServicosConfiguration : IEntityTypeConfiguration<TabelaIrrfServicos>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TabelaIrrfServicos> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TabelasIrrfServicos");
        builder.HasKey(tabela => tabela.Id);
        builder.Property(tabela => tabela.Id)
            .HasConversion(id => id.Value, value => new TabelaIrrfServicosId(value))
            .ValueGeneratedNever();

        builder.Property(tabela => tabela.VigenciaInicio).HasColumnType("date");
        builder.Property(tabela => tabela.VigenciaFim).HasColumnType("date");
        builder.Property(tabela => tabela.ValorMinimoRetencao).HasColumnType("decimal(18,2)");

        builder.OwnsMany(tabela => tabela.Faixas, faixas =>
        {
            faixas.ToTable("FaixasIrrfServicos");
            faixas.WithOwner().HasForeignKey("TabelaIrrfServicosId");
            faixas.Property<int>("Id").ValueGeneratedOnAdd();
            faixas.HasKey("Id");
            faixas.Property(faixa => faixa.Codigo).HasMaxLength(40).IsRequired();
            faixas.Property(faixa => faixa.Descricao).HasMaxLength(200).IsRequired();
            faixas.Property(faixa => faixa.Aliquota).HasColumnType("decimal(9,6)");
            faixas.Property(faixa => faixa.CodigoReceitaDarf).HasMaxLength(10).IsRequired();
        });
        builder.Navigation(tabela => tabela.Faixas).Metadata.SetField("_faixas");
        builder.Navigation(tabela => tabela.Faixas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(tabela => new { tabela.TenantId, tabela.VigenciaInicio }).IsUnique();
    }
}
