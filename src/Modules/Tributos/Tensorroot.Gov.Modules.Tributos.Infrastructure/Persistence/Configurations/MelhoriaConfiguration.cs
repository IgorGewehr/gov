using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Melhoria;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="ObraContribuicaoMelhoria"/> e seus imóveis beneficiados.</summary>
public sealed class ObraContribuicaoMelhoriaConfiguration : IEntityTypeConfiguration<ObraContribuicaoMelhoria>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ObraContribuicaoMelhoria> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ObrasContribuicaoMelhoria");
        builder.HasKey(obra => obra.Id);
        builder.Property(obra => obra.Id)
            .HasConversion(id => id.Value, value => new ObraContribuicaoMelhoriaId(value))
            .ValueGeneratedNever();

        builder.Property(obra => obra.IdentificacaoObra).HasMaxLength(120).IsRequired();
        builder.Property(obra => obra.MemorialDescritivo).HasMaxLength(4000).IsRequired();
        builder.Property(obra => obra.CustoTotalObra)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(obra => obra.ParcelaCustoFinanciadaPercentual).HasColumnType("decimal(9,4)");
        builder.Property(obra => obra.ZonaBeneficiada).HasMaxLength(200).IsRequired();
        builder.Property(obra => obra.FatorAbsorcaoPercentual).HasColumnType("decimal(9,4)");
        builder.Property(obra => obra.DataPublicacaoEdital);
        builder.Property(obra => obra.FimPrazoImpugnacao);
        builder.Property(obra => obra.FundamentoLegal).HasMaxLength(300).IsRequired();
        builder.Property(obra => obra.Estado).HasConversion<string>().HasMaxLength(30);

        // LimiteTotal é derivado (não persistido).
        builder.Ignore(obra => obra.LimiteTotal);

        builder.HasMany(obra => obra.Imoveis).WithOne().HasForeignKey(i => i.ObraId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(obra => obra.Imoveis).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(obra => new { obra.TenantId, obra.IdentificacaoObra });
    }
}

/// <summary>Mapeamento EF Core da entidade <see cref="ImovelBeneficiado"/>.</summary>
public sealed class ImovelBeneficiadoConfiguration : IEntityTypeConfiguration<ImovelBeneficiado>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ImovelBeneficiado> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ImoveisBeneficiadosMelhoria");
        builder.HasKey(imovel => imovel.Id);
        builder.Property(imovel => imovel.Id)
            .HasConversion(id => id.Value, value => new ImovelBeneficiadoId(value))
            .ValueGeneratedNever();

        builder.Property(imovel => imovel.ObraId)
            .HasConversion(id => id.Value, value => new ObraContribuicaoMelhoriaId(value));
        builder.Property(imovel => imovel.ImovelId)
            .HasConversion(id => id.Value, value => new ImovelId(value));
        builder.Property(imovel => imovel.ProprietarioId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));

        builder.Property(imovel => imovel.ValorizacaoIndividual)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(imovel => imovel.ContribuicaoRateada)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");

        builder.HasIndex(imovel => imovel.ObraId);
    }
}
