using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="TabelaAliquotaIptu"/> e suas faixas.</summary>
public sealed class TabelaAliquotaIptuConfiguration : IEntityTypeConfiguration<TabelaAliquotaIptu>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TabelaAliquotaIptu> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TabelasAliquotaIptu");
        builder.HasKey(tabela => tabela.Id);
        builder.Property(tabela => tabela.Id)
            .HasConversion(id => id.Value, value => new TabelaAliquotaIptuId(value))
            .ValueGeneratedNever();

        builder.Property(tabela => tabela.Exercicio);
        builder.Property(tabela => tabela.Edificado);
        builder.Property(tabela => tabela.FundamentoLegal).HasMaxLength(300).IsRequired();
        builder.Property(tabela => tabela.Vigente);

        builder.HasMany(tabela => tabela.Faixas).WithOne().HasForeignKey(f => f.TabelaAliquotaIptuId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(tabela => tabela.Faixas).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(tabela => new { tabela.TenantId, tabela.Exercicio, tabela.Edificado });
    }
}

/// <summary>Mapeamento EF Core da entidade <see cref="FaixaAliquotaIptu"/>.</summary>
public sealed class FaixaAliquotaIptuConfiguration : IEntityTypeConfiguration<FaixaAliquotaIptu>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FaixaAliquotaIptu> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("FaixasAliquotaIptu");
        builder.HasKey(faixa => faixa.Id);
        builder.Property(faixa => faixa.Id)
            .HasConversion(id => id.Value, value => new FaixaAliquotaIptuId(value))
            .ValueGeneratedNever();

        builder.Property(faixa => faixa.TabelaAliquotaIptuId)
            .HasConversion(id => id.Value, value => new TabelaAliquotaIptuId(value));

        builder.Property(faixa => faixa.ValorVenalMinimo).HasColumnType("decimal(18,2)");
        builder.Property(faixa => faixa.ValorVenalMaximo).HasColumnType("decimal(18,2)");
        builder.Property(faixa => faixa.AliquotaPercentual).HasColumnType("decimal(9,4)");

        builder.HasIndex(faixa => faixa.TabelaAliquotaIptuId);
    }
}
