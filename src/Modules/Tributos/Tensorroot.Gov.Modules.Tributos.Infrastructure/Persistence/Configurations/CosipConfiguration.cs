using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Cosip;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="TabelaCosip"/> e suas faixas.</summary>
public sealed class TabelaCosipConfiguration : IEntityTypeConfiguration<TabelaCosip>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TabelaCosip> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TabelasCosip");
        builder.HasKey(tabela => tabela.Id);
        builder.Property(tabela => tabela.Id)
            .HasConversion(id => id.Value, value => new TabelaCosipId(value))
            .ValueGeneratedNever();

        builder.Property(tabela => tabela.Exercicio);
        builder.Property(tabela => tabela.FundamentoLegal).HasMaxLength(300).IsRequired();
        builder.Property(tabela => tabela.Vigente);

        builder.HasMany(tabela => tabela.Faixas).WithOne().HasForeignKey(f => f.TabelaCosipId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(tabela => tabela.Faixas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(tabela => new { tabela.TenantId, tabela.Exercicio });
    }
}

/// <summary>Mapeamento EF Core da entidade <see cref="FaixaCosip"/>.</summary>
public sealed class FaixaCosipConfiguration : IEntityTypeConfiguration<FaixaCosip>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FaixaCosip> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("FaixasCosip");
        builder.HasKey(faixa => faixa.Id);
        builder.Property(faixa => faixa.Id)
            .HasConversion(id => id.Value, value => new FaixaCosipId(value))
            .ValueGeneratedNever();

        builder.Property(faixa => faixa.TabelaCosipId)
            .HasConversion(id => id.Value, value => new TabelaCosipId(value));

        builder.Property(faixa => faixa.Classe).HasConversion<string>().HasMaxLength(20);
        builder.Property(faixa => faixa.ConsumoMinimoKwh).HasColumnType("decimal(18,4)");
        builder.Property(faixa => faixa.ConsumoMaximoKwh).HasColumnType("decimal(18,4)");
        builder.Property(faixa => faixa.Valor)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");

        builder.HasIndex(faixa => faixa.TabelaCosipId);
    }
}
