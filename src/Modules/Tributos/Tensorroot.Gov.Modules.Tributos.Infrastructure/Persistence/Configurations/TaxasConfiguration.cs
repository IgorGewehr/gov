using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Taxas;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="TabelaTaxa"/> e suas faixas.</summary>
public sealed class TabelaTaxaConfiguration : IEntityTypeConfiguration<TabelaTaxa>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TabelaTaxa> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TabelasTaxa");
        builder.HasKey(tabela => tabela.Id);
        builder.Property(tabela => tabela.Id)
            .HasConversion(id => id.Value, value => new TabelaTaxaId(value))
            .ValueGeneratedNever();

        builder.Property(tabela => tabela.Codigo).HasMaxLength(40).IsRequired();
        builder.Property(tabela => tabela.Descricao).HasMaxLength(200).IsRequired();
        builder.Property(tabela => tabela.Especie).HasConversion<string>().HasMaxLength(40);
        builder.Property(tabela => tabela.ModoCalculo).HasConversion<string>().HasMaxLength(20);
        builder.Property(tabela => tabela.Exercicio);
        builder.Property(tabela => tabela.ValorBase)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(tabela => tabela.FundamentoLegal).HasMaxLength(300).IsRequired();
        builder.Property(tabela => tabela.Vigente);

        builder.HasMany(tabela => tabela.Faixas).WithOne().HasForeignKey(f => f.TabelaTaxaId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(tabela => tabela.Faixas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(tabela => new { tabela.TenantId, tabela.Codigo, tabela.Exercicio });
    }
}

/// <summary>Mapeamento EF Core da entidade <see cref="FaixaTaxa"/>.</summary>
public sealed class FaixaTaxaConfiguration : IEntityTypeConfiguration<FaixaTaxa>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FaixaTaxa> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("FaixasTaxa");
        builder.HasKey(faixa => faixa.Id);
        builder.Property(faixa => faixa.Id)
            .HasConversion(id => id.Value, value => new FaixaTaxaId(value))
            .ValueGeneratedNever();

        builder.Property(faixa => faixa.TabelaTaxaId)
            .HasConversion(id => id.Value, value => new TabelaTaxaId(value));

        builder.Property(faixa => faixa.LimiteInferior).HasColumnType("decimal(18,4)");
        builder.Property(faixa => faixa.LimiteSuperior).HasColumnType("decimal(18,4)");
        builder.Property(faixa => faixa.Valor)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");

        builder.HasIndex(faixa => faixa.TabelaTaxaId);
    }
}
