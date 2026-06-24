using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="TabelaTemporalidade"/> e suas regras.</summary>
public sealed class TabelaTemporalidadeConfiguration : IEntityTypeConfiguration<TabelaTemporalidade>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TabelaTemporalidade> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TabelasTemporalidade");
        builder.HasKey(ttd => ttd.Id);
        builder.Property(ttd => ttd.Id)
            .HasConversion(id => id.Value, value => new TabelaTemporalidadeId(value))
            .ValueGeneratedNever();

        builder.Property(ttd => ttd.Nome).HasMaxLength(200).IsRequired();
        builder.Property(ttd => ttd.Ativa);

        builder.OwnsMany(ttd => ttd.Regras, MapearRegras);
        builder.Navigation(ttd => ttd.Regras).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(ttd => new { ttd.TenantId, ttd.Ativa });
    }

    private static void MapearRegras(OwnedNavigationBuilder<TabelaTemporalidade, RegraTemporalidade> regras)
    {
        regras.ToTable("RegrasTemporalidade");
        regras.WithOwner().HasForeignKey("TabelaTemporalidadeId");
        regras.HasKey(regra => regra.Id);
        regras.Property(regra => regra.Id)
            .HasConversion(id => id.Value, value => new RegraTemporalidadeId(value))
            .ValueGeneratedNever();

        regras.Property(regra => regra.CodigoClassificacao).HasMaxLength(ClasseDocumental.ComprimentoMaximoCodigo).IsRequired();
        regras.Property(regra => regra.PrazoGuardaCorrenteAnos);
        regras.Property(regra => regra.PrazoGuardaIntermediariaAnos);
        regras.Property(regra => regra.DestinacaoFinal).HasConversion<string>().HasMaxLength(20);
        regras.Property(regra => regra.EventoContagem).HasConversion<string>().HasMaxLength(30);
        regras.Property(regra => regra.Observacao).HasMaxLength(RegraTemporalidade.ComprimentoMaximoObservacao);

        regras.HasIndex("TabelaTemporalidadeId", nameof(RegraTemporalidade.CodigoClassificacao));
    }
}
