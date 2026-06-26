using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="DispensaEletronica"/> e de suas entidades filhas.</summary>
public sealed class DispensaConfiguration : IEntityTypeConfiguration<DispensaEletronica>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DispensaEletronica> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Dispensas");
        builder.HasKey(dispensa => dispensa.Id);
        builder.Property(dispensa => dispensa.Id)
            .HasConversion(id => id.Value, value => new DispensaEletronicaId(value))
            .ValueGeneratedNever();

        builder.Property(dispensa => dispensa.Objeto).HasMaxLength(2000);
        builder.Property(dispensa => dispensa.Fundamento).HasConversion<string>().HasMaxLength(40);
        builder.Property(dispensa => dispensa.CriterioJulgamento).HasConversion<string>().HasMaxLength(20);
        builder.Property(dispensa => dispensa.Situacao).HasConversion<string>().HasMaxLength(20);
        builder.Property(dispensa => dispensa.NumeroAviso).HasMaxLength(60);
        builder.Property(dispensa => dispensa.NumeroPncp).HasMaxLength(60);
        builder.Property(dispensa => dispensa.LimiteLegalNormaFonte).HasMaxLength(200);
        builder.Property(dispensa => dispensa.AberturaDisputa);

        builder.Property(dispensa => dispensa.LimiteLegalVigente)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");

        builder.OwnsMany(dispensa => dispensa.Itens, MapearItens);
        builder.OwnsMany(dispensa => dispensa.Cotacoes, MapearCotacoes);

        builder.Navigation(dispensa => dispensa.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(dispensa => dispensa.Cotacoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(dispensa => new { dispensa.TenantId, dispensa.Situacao });
    }

    private static void MapearItens(OwnedNavigationBuilder<DispensaEletronica, ItemDispensa> itens)
    {
        itens.ToTable("DispensasItens");
        itens.WithOwner().HasForeignKey("DispensaId");
        itens.HasKey(item => item.Id);
        itens.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ItemDispensaId(value))
            .ValueGeneratedNever();
        itens.Property(item => item.Descricao).HasMaxLength(2000);
        itens.Property(item => item.Quantidade).HasColumnType("decimal(18,4)");
        itens.Property(item => item.ValorUnitarioEstimado)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        itens.Ignore(item => item.ValorTotalEstimado);
    }

    private static void MapearCotacoes(OwnedNavigationBuilder<DispensaEletronica, CotacaoDispensa> cotacoes)
    {
        cotacoes.ToTable("DispensasCotacoes");
        cotacoes.WithOwner().HasForeignKey("DispensaId");
        cotacoes.HasKey(cotacao => cotacao.Id);
        cotacoes.Property(cotacao => cotacao.Id)
            .HasConversion(id => id.Value, value => new CotacaoDispensaId(value))
            .ValueGeneratedNever();
        cotacoes.Property(cotacao => cotacao.ItemId)
            .HasConversion(id => id.Value, value => new ItemDispensaId(value));
        cotacoes.Property(cotacao => cotacao.Situacao).HasConversion<string>().HasMaxLength(20);
        cotacoes.Property(cotacao => cotacao.DataRegistro);
        cotacoes.Property(cotacao => cotacao.Sequencia);
        cotacoes.Property(cotacao => cotacao.Valor)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
    }
}
