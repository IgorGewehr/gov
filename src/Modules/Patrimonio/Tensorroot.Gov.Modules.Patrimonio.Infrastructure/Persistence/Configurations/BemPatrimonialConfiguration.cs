using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="BemPatrimonial"/> e de suas entidades filhas.</summary>
public sealed class BemPatrimonialConfiguration : IEntityTypeConfiguration<BemPatrimonial>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BemPatrimonial> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Bens");
        builder.HasKey(bem => bem.Id);
        builder.Property(bem => bem.Id)
            .HasConversion(id => id.Value, value => new BemPatrimonialId(value))
            .ValueGeneratedNever();

        builder.Property(bem => bem.Descricao).HasMaxLength(200);
        builder.Property(bem => bem.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(bem => bem.Situacao).HasConversion<string>().HasMaxLength(30);

        builder.Property(bem => bem.NumeroTombamento)
            .HasConversion(
                tombo => tombo!.Value.Valor,
                valor => NumeroTombamento.De(valor))
            .HasMaxLength(NumeroTombamento.ComprimentoMaximo);

        builder.Property(bem => bem.ValorInicial)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(bem => bem.ValorResidual)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(bem => bem.ValorContabil)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(bem => bem.ValorTerreno).HasColumnType("decimal(18,2)");

        // Propriedades calculadas (sem coluna).
        builder.Ignore(bem => bem.ValorDepreciavel);
        builder.Ignore(bem => bem.ParcelaMensal);

        builder.OwnsMany(bem => bem.Movimentacoes, MapearMovimentacoes);
        builder.OwnsMany(bem => bem.HistoricosDepreciacao, MapearHistoricos);
        builder.OwnsMany(bem => bem.Reavaliacoes, MapearReavaliacoes);
        builder.OwnsMany(bem => bem.Impairments, MapearImpairments);

        builder.Navigation(bem => bem.Movimentacoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(bem => bem.HistoricosDepreciacao).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(bem => bem.Reavaliacoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(bem => bem.Impairments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(bem => new { bem.TenantId, bem.NumeroTombamento });
    }

    private static void MapearMovimentacoes(OwnedNavigationBuilder<BemPatrimonial, MovimentacaoPatrimonial> movimentacoes)
    {
        movimentacoes.ToTable("BensMovimentacoes");
        movimentacoes.WithOwner().HasForeignKey("BemPatrimonialId");
        movimentacoes.HasKey(movimentacao => movimentacao.Id);
        movimentacoes.Property(movimentacao => movimentacao.Id)
            .HasConversion(id => id.Value, value => new MovimentacaoPatrimonialId(value))
            .ValueGeneratedNever();
        movimentacoes.Property(movimentacao => movimentacao.LocalizacaoOrigem).HasMaxLength(200);
        movimentacoes.Property(movimentacao => movimentacao.LocalizacaoDestino).HasMaxLength(200);
    }

    private static void MapearHistoricos(OwnedNavigationBuilder<BemPatrimonial, HistoricoDepreciacao> historicos)
    {
        historicos.ToTable("BensHistoricosDepreciacao");
        historicos.WithOwner().HasForeignKey("BemPatrimonialId");
        historicos.HasKey(historico => historico.Id);
        historicos.Property(historico => historico.Id)
            .HasConversion(id => id.Value, value => new HistoricoDepreciacaoId(value))
            .ValueGeneratedNever();
        historicos.Property(historico => historico.ValorContabilResultante)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        historicos.Property(historico => historico.ValorDepreciado).HasColumnType("decimal(18,2)");
    }

    private static void MapearReavaliacoes(OwnedNavigationBuilder<BemPatrimonial, Reavaliacao> reavaliacoes)
    {
        reavaliacoes.ToTable("BensReavaliacoes");
        reavaliacoes.WithOwner().HasForeignKey("BemPatrimonialId");
        reavaliacoes.HasKey(reavaliacao => reavaliacao.Id);
        reavaliacoes.Property(reavaliacao => reavaliacao.Id)
            .HasConversion(id => id.Value, value => new ReavaliacaoId(value))
            .ValueGeneratedNever();
        reavaliacoes.Property(reavaliacao => reavaliacao.NovoValorJusto).HasColumnType("decimal(18,2)");
        reavaliacoes.Property(reavaliacao => reavaliacao.LaudoUri).HasMaxLength(500);
    }

    private static void MapearImpairments(OwnedNavigationBuilder<BemPatrimonial, Impairment> impairments)
    {
        impairments.ToTable("BensImpairments");
        impairments.WithOwner().HasForeignKey("BemPatrimonialId");
        impairments.HasKey(impairment => impairment.Id);
        impairments.Property(impairment => impairment.Id)
            .HasConversion(id => id.Value, value => new ImpairmentId(value))
            .ValueGeneratedNever();
        impairments.Property(impairment => impairment.ValorRecuperavel).HasColumnType("decimal(18,2)");
        impairments.Property(impairment => impairment.PerdaReconhecida).HasColumnType("decimal(18,2)");
        impairments.Property(impairment => impairment.LaudoUri).HasMaxLength(500);
    }
}
