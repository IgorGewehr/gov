using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Merenda;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="Cardapio"/> e da entidade-filha <see cref="ItemCardapio"/>.</summary>
public sealed class CardapioConfiguration : IEntityTypeConfiguration<Cardapio>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Cardapio> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Cardapios");
        builder.HasKey(cardapio => cardapio.Id);
        builder.Property(cardapio => cardapio.Id)
            .HasConversion(id => id.Value, value => new CardapioId(value))
            .ValueGeneratedNever();

        builder.Property(cardapio => cardapio.EscolaId)
            .HasConversion(id => id.Value, value => new EscolaId(value));

        builder.Property(cardapio => cardapio.FaixaEtaria).HasConversion<string>().HasMaxLength(40);
        builder.Property(cardapio => cardapio.Semana);
        builder.Property(cardapio => cardapio.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.OwnsMany(cardapio => cardapio.Itens, MapearItens);
        builder.Navigation(cardapio => cardapio.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(cardapio => new { cardapio.TenantId, cardapio.EscolaId, cardapio.Semana, cardapio.FaixaEtaria });
    }

    private static void MapearItens(OwnedNavigationBuilder<Cardapio, ItemCardapio> itens)
    {
        itens.ToTable("CardapiosItens");
        itens.WithOwner().HasForeignKey("CardapioId");
        itens.HasKey(item => item.Id);
        itens.Property(item => item.Id)
            .HasConversion(id => id.Value, value => new ItemCardapioId(value))
            .ValueGeneratedNever();
        itens.Property(item => item.Dia).HasConversion<string>().HasMaxLength(20);
        itens.Property(item => item.Refeicao).HasConversion<string>().HasMaxLength(20);
        itens.Property(item => item.GeneroEstoqueId);
        itens.Property(item => item.QuantidadePerCapita).HasColumnType("decimal(12,4)");
        itens.Property(item => item.UnidadeMedida).HasMaxLength(20);
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="DistribuicaoMerenda"/> e da entidade-filha <see cref="ConsumoGenero"/>.</summary>
public sealed class DistribuicaoMerendaConfiguration : IEntityTypeConfiguration<DistribuicaoMerenda>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DistribuicaoMerenda> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("DistribuicoesMerenda");
        builder.HasKey(distribuicao => distribuicao.Id);
        builder.Property(distribuicao => distribuicao.Id)
            .HasConversion(id => id.Value, value => new DistribuicaoMerendaId(value))
            .ValueGeneratedNever();

        builder.Property(distribuicao => distribuicao.EscolaId)
            .HasConversion(id => id.Value, value => new EscolaId(value));
        builder.Property(distribuicao => distribuicao.CardapioId)
            .HasConversion(id => id.Value, value => new CardapioId(value));

        builder.Property(distribuicao => distribuicao.Data);
        builder.Property(distribuicao => distribuicao.Refeicao).HasConversion<string>().HasMaxLength(20);
        builder.Property(distribuicao => distribuicao.Comensais);

        builder.OwnsMany(distribuicao => distribuicao.Consumos, MapearConsumos);
        builder.Navigation(distribuicao => distribuicao.Consumos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(distribuicao => new { distribuicao.TenantId, distribuicao.EscolaId, distribuicao.Data });
    }

    private static void MapearConsumos(OwnedNavigationBuilder<DistribuicaoMerenda, ConsumoGenero> consumos)
    {
        consumos.ToTable("DistribuicoesMerendaConsumos");
        consumos.WithOwner().HasForeignKey("DistribuicaoMerendaId");
        consumos.HasKey(consumo => consumo.Id);
        consumos.Property(consumo => consumo.Id)
            .HasConversion(id => id.Value, value => new ConsumoGeneroId(value))
            .ValueGeneratedNever();
        consumos.Property(consumo => consumo.GeneroEstoqueId);
        consumos.Property(consumo => consumo.Quantidade).HasColumnType("decimal(14,4)");
        consumos.Property(consumo => consumo.UnidadeMedida).HasMaxLength(20);
    }
}
