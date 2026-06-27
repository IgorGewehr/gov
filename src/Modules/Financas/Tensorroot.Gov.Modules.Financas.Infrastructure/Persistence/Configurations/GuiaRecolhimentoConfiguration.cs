using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Recolhimentos;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="GuiaRecolhimento"/> e seus itens.</summary>
public sealed class GuiaRecolhimentoConfiguration : IEntityTypeConfiguration<GuiaRecolhimento>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GuiaRecolhimento> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("GuiasRecolhimento");
        builder.HasKey(guia => guia.Id);
        builder.Property(guia => guia.Id)
            .HasConversion(id => id.Value, value => new GuiaRecolhimentoId(value))
            .ValueGeneratedNever();

        builder.Property(guia => guia.Natureza).HasConversion<string>().HasMaxLength(30);
        builder.Property(guia => guia.CodigoReceita).HasMaxLength(20);
        builder.Property(guia => guia.FavorecidoDocumento).HasMaxLength(20);
        builder.Property(guia => guia.DataVencimento).HasColumnType("date");
        builder.Property(guia => guia.Competencia).HasColumnType("date");
        builder.Property(guia => guia.DataRecolhimento).HasColumnType("date");
        builder.Property(guia => guia.ValorTotal)
            .HasConversion(valor => valor.Valor, valor => ValorMonetario.De(valor))
            .HasColumnType("decimal(18,2)");
        builder.Property(guia => guia.Situacao).HasConversion<string>().HasMaxLength(20);

        builder.OwnsMany(guia => guia.Itens, itens =>
        {
            itens.ToTable("ItensGuiaRecolhimento");
            itens.WithOwner().HasForeignKey("GuiaRecolhimentoId");
            itens.Property<int>("Id").ValueGeneratedOnAdd();
            itens.HasKey("Id");
            itens.Property(item => item.LiquidacaoId)
                .HasConversion(id => id.Value, value => new LiquidacaoId(value));
            itens.Property(item => item.RetencaoId)
                .HasConversion(id => id.Value, value => new RetencaoId(value));
            itens.Property(item => item.Valor).HasColumnType("decimal(18,2)");
            itens.HasIndex(item => item.RetencaoId);
        });
        builder.Navigation(guia => guia.Itens).Metadata.SetField("_itens");
        builder.Navigation(guia => guia.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(guia => new { guia.TenantId, guia.Situacao });
    }
}
