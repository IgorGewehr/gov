using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento EF Core do agregado <see cref="AliquotaItbi"/>.</summary>
public sealed class AliquotaItbiConfiguration : IEntityTypeConfiguration<AliquotaItbi>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AliquotaItbi> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AliquotasItbi");
        builder.HasKey(aliquota => aliquota.Id);
        builder.Property(aliquota => aliquota.Id)
            .HasConversion(id => id.Value, value => new AliquotaItbiId(value))
            .ValueGeneratedNever();

        builder.Property(aliquota => aliquota.Exercicio);
        builder.Property(aliquota => aliquota.AliquotaGeralPercentual).HasColumnType("decimal(9,4)");
        builder.Property(aliquota => aliquota.AliquotaSfhFinanciadaPercentual).HasColumnType("decimal(9,4)");
        builder.Property(aliquota => aliquota.FundamentoLegal).HasMaxLength(300).IsRequired();
        builder.Property(aliquota => aliquota.Vigente);

        builder.HasIndex(aliquota => new { aliquota.TenantId, aliquota.Exercicio });
    }
}

/// <summary>Mapeamento EF Core do agregado <see cref="TransmissaoImobiliaria"/>.</summary>
public sealed class TransmissaoImobiliariaConfiguration : IEntityTypeConfiguration<TransmissaoImobiliaria>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TransmissaoImobiliaria> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TransmissoesImobiliarias");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new TransmissaoImobiliariaId(value))
            .ValueGeneratedNever();

        builder.Property(t => t.ImovelId)
            .HasConversion(id => id.Value, value => new ImovelId(value));
        builder.Property(t => t.TransmitenteId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));
        builder.Property(t => t.AdquirenteId)
            .HasConversion(id => id.Value, value => new ContribuinteId(value));

        builder.Property(t => t.Exercicio);
        builder.Property(t => t.ValorDeclarado)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(t => t.ValorVenalReferencia)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(t => t.BaseCalculo)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");
        builder.Property(t => t.BaseFoiValorVenal);
        builder.Property(t => t.AliquotaPercentual).HasColumnType("decimal(9,4)");
        builder.Property(t => t.ImpostoDevido)
            .HasConversion(v => v.Valor, v => ValorMonetario.De(v)).HasColumnType("decimal(18,2)");

        builder.HasIndex(t => new { t.TenantId, t.ImovelId });
    }
}
